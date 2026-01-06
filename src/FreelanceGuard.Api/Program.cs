using FreelanceGuard.Api.Middleware;
using FreelanceGuard.Api.Repositories;
using FreelanceGuard.Api.Services;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// --- 1. SERVICE CONFIGURATION (Dependency Injection) ---

// Redis Configuration
var redisConnection = builder.Configuration.GetConnectionString("Redis")
    ?? throw new InvalidOperationException("Redis connection string is not configured.");
builder.Services.AddSingleton<IConnectionMultiplexer>(sp => 
    ConnectionMultiplexer.Connect(redisConnection));

// HTTP Client and License Service (Background)
builder.Services.AddHttpClient();
builder.Services.AddHostedService<LicenseCheckerService>();

// Register Repository
builder.Services.AddScoped<IEmployeeRepository, EmployeeRepository>();

builder.Services.AddControllers();

var app = builder.Build();

// --- 2. REQUEST PIPELINE (Middleware) ---

// ACTIVATE LICENSE GUARD
// Must be placed before MapControllers to protect all endpoints
app.UseMiddleware<LicenseGuardMiddleware>();

app.MapControllers();
app.MapGet("/health", () => "System OK"); 

// --- 3. DATABASE INITIALIZATION ---
// This creates the table automatically on startup (for development/demo only)
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var repository = services.GetRequiredService<IEmployeeRepository>();
        // Brief wait to allow SQL Server to start in Docker
        Console.WriteLine("⏳ Esperando a SQL Server...");
        Thread.Sleep(5000); 
        
        await repository.InitDatabaseAsync();
        Console.WriteLine("✅ Base de datos inicializada.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"⚠️ Error inicializando BD (puede que SQL Server siga cargando): {ex.Message}");
    }
}

app.Run();