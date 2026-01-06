using System.Text.Json;
using StackExchange.Redis;

namespace FreelanceGuard.Api.Services;

/// <summary>
/// Represents the license configuration retrieved from the remote Gist.
/// This record is used to deserialize the JSON response from the GitHub Gist.
/// </summary>
/// <param name="IsValid">Indicates whether the license is currently valid.</param>
/// <param name="Message">A message describing the license status.</param>
public record LicenseConfig(bool IsValid, string Message);

/// <summary>
/// Background service that implements a "Dead Man's Switch" mechanism for remote license validation.
/// This service periodically checks a GitHub Gist for license validity and updates Redis accordingly.
/// 
/// <para>
/// <strong>DRM (Digital Rights Management) Component:</strong> This service is part of a defensive
/// licensing system that enables remote control and blocking of the application. If the license
/// check fails or the remote endpoint is unreachable, the system defaults to a "fail-secure" state
/// by blocking access to protect against unauthorized usage.
/// </para>
/// 
/// <para>
/// The service runs continuously in the background, checking the license status at configurable intervals
/// and updating the Redis cache with the current license state. This allows the middleware to quickly
/// determine if requests should be allowed or blocked without making external HTTP calls on every request.
/// </para>
/// </summary>
public class LicenseCheckerService : BackgroundService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<LicenseCheckerService> _logger;
    private readonly string _licenseUrl;
    private readonly int _checkIntervalMin;

    /// <summary>
    /// Initializes a new instance of the <see cref="LicenseCheckerService"/> class.
    /// </summary>
    /// <param name="httpClientFactory">Factory for creating HTTP clients to query the license endpoint.</param>
    /// <param name="redis">Redis connection multiplexer for caching license status.</param>
    /// <param name="config">Configuration instance to retrieve license check settings.</param>
    /// <param name="logger">Logger instance for recording license check events.</param>
    /// <exception cref="ArgumentNullException">Thrown when the license URL is not configured.</exception>
    public LicenseCheckerService(
        IHttpClientFactory httpClientFactory,
        IConnectionMultiplexer redis,
        IConfiguration config,
        ILogger<LicenseCheckerService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _redis = redis;
        _logger = logger;
        _licenseUrl = config["LICENSE_CHECK_URL"] ?? throw new ArgumentNullException(nameof(config), "URL de licencia no configurada");
        _checkIntervalMin = int.Parse(config["LICENSE_CHECK_INTERVAL_MIN"] ?? "10");
    }

    /// <summary>
    /// Executes the background license checking loop.
    /// This method runs continuously, checking the license status at configured intervals
    /// and updating Redis with the current state. Implements fail-secure behavior:
    /// if the license check fails or times out, the system is blocked.
    /// </summary>
    /// <param name="stoppingToken">Cancellation token to stop the service gracefully.</param>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🕵️ Servicio de Licencias Iniciado. Objetivo: {Url}", _licenseUrl);

        while (!stoppingToken.IsCancellationRequested)
        {
            bool isLicenseActive = false;
            string statusMessage = "System Locked due to connectivity issues.";

            try
            {
                var client = _httpClientFactory.CreateClient();
                // Short timeout: if GitHub doesn't respond in 5s, we assume the worst (fail-secure)
                client.Timeout = TimeSpan.FromSeconds(5); 
                
                var response = await client.GetStringAsync(_licenseUrl, stoppingToken);
                var config = JsonSerializer.Deserialize<LicenseConfig>(response, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (config != null)
                {
                    isLicenseActive = config.IsValid;
                    statusMessage = config.Message;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("❌ Error verificando licencia: {Message}. Se asume BLOQUEO por seguridad.", ex.Message);
                isLicenseActive = false; // Fail-Secure: When in doubt, block access.
            }

            // Save state in Redis
            var db = _redis.GetDatabase();
            
            // If valid, save "OK". If not, save "BLOCKED".
            // Use a TTL slightly longer than the interval (e.g., +2 min) to provide margin for momentary network failures.
            if (isLicenseActive)
            {
                await db.StringSetAsync("license_status", "OK", TimeSpan.FromMinutes(_checkIntervalMin + 2));
                _logger.LogInformation("✅ Licencia Verificada: ACTIVA");
            }
            else
            {
                // If license is invalid, force immediate blocking
                await db.StringSetAsync("license_status", "BLOCKED", TimeSpan.FromMinutes(_checkIntervalMin + 2));
                _logger.LogWarning("⛔ Licencia DENEGADA o Error de Red. Sistema Bloqueado.");
            }

            // Wait X minutes before the next check
            await Task.Delay(TimeSpan.FromMinutes(_checkIntervalMin), stoppingToken);
        }
    }
}