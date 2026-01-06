using StackExchange.Redis;

namespace FreelanceGuard.Api.Middleware;

/// <summary>
/// Middleware that implements the license guard mechanism for the defensive REST API.
/// This middleware intercepts all HTTP requests (except health checks) and validates
/// the license status stored in Redis before allowing access to protected endpoints.
/// 
/// <para>
/// <strong>DRM (Digital Rights Management) Component:</strong> This middleware is a critical
/// part of the remote license control system. It enforces access restrictions based on
/// the license status cached in Redis, which is periodically updated by the
/// <see cref="Services.LicenseCheckerService"/>. This enables remote blocking of the
/// application without requiring code changes or redeployment.
/// </para>
/// 
/// <para>
/// If the license status is not "OK", the middleware immediately returns HTTP 402
/// (Payment Required) and prevents the request from reaching the controllers,
/// effectively blocking all API functionality.
/// </para>
/// </summary>
public class LicenseGuardMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<LicenseGuardMiddleware> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="LicenseGuardMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next middleware in the request pipeline.</param>
    /// <param name="redis">Redis connection multiplexer to check license status.</param>
    /// <param name="logger">Logger instance for recording blocked access attempts.</param>
    public LicenseGuardMiddleware(RequestDelegate next, IConnectionMultiplexer redis, ILogger<LicenseGuardMiddleware> logger)
    {
        _next = next;
        _redis = redis;
        _logger = logger;
    }

    /// <summary>
    /// Invokes the middleware to check license status before processing the request.
    /// Health check endpoints are exempt from license validation to allow monitoring
    /// of the system status even when blocked.
    /// </summary>
    /// <param name="context">The HTTP context for the current request.</param>
    /// <returns>A task that represents the asynchronous middleware operation.</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        // Skip license check for health endpoints to allow monitoring even when blocked
        if (context.Request.Path.StartsWithSegments("/health"))
        {
            await _next(context);
            return;
        }

        var db = _redis.GetDatabase();
        var status = await db.StringGetAsync("license_status");

        if (status != "OK")
        {
            _logger.LogWarning("🚫 Intento de acceso bloqueado. Estado de licencia: {Status}", status);
            
            context.Response.StatusCode = 402; // Payment Required
            context.Response.ContentType = "application/json";
            
            // Professional but firm message
            await context.Response.WriteAsync("{\"error\": \"License validation failed. Please contact the software provider.\", \"code\": 402}");
            return; // 🛑 REQUEST BLOCKED HERE. Does not reach the controller.
        }

        // If everything is OK, allow the request to proceed
        await _next(context);
    }
}