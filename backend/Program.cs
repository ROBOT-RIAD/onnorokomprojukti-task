using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SchoolAdmission.Api.Data;
using SchoolAdmission.Api.Middleware;
using SchoolAdmission.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel to listen on port 8080 as required by spec
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(8080);
});

// Configure Database Connection
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? Environment.GetEnvironmentVariable("DATABASE_URL")
    ?? Environment.GetEnvironmentVariable("DB_CONNECTION_STRING")
    ?? "Host=localhost;Port=5432;Database=school_admission;Username=postgres;Password=postgres";

// If DATABASE_URL is in postgres:// format, convert to Npgsql format
if (connectionString.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) ||
    connectionString.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
{
    var uri = new Uri(connectionString);
    var userInfo = uri.UserInfo.Split(':');
    var user = userInfo[0];
    var password = userInfo.Length > 1 ? userInfo[1] : "";
    var port = uri.Port > 0 ? uri.Port : 5432;
    var database = uri.AbsolutePath.TrimStart('/');
    connectionString = $"Host={uri.Host};Port={port};Database={database};Username={user};Password={password};";
}

builder.Services.AddDbContext<SchoolDbContext>(options =>
{
    options.UseNpgsql(connectionString);
});

// Services
builder.Services.AddScoped<IAdmissionService, AdmissionService>();
builder.Services.AddHostedService<HoldCleanupBackgroundService>();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader()
              .WithExposedHeaders("X-Student-Id");
    });
});

// Controllers & JSON configuration
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.WriteIndented = false;
    });

builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

// Auto-create database with retry logic on startup (for Docker Compose startup readiness)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<SchoolDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    if (db.Database.IsRelational())
    {
        var retries = 0;
        const int maxRetries = 15;
        while (retries < maxRetries)
        {
            try
            {
                logger.LogInformation("Attempting database initialization (Attempt {Attempt}/{MaxRetries})...", retries + 1, maxRetries);
                db.Database.EnsureCreated();
                logger.LogInformation("Database initialized successfully.");
                break;
            }
            catch (Exception ex)
            {
                retries++;
                logger.LogWarning("Database connection attempt failed: {Message}. Retrying in 2 seconds...", ex.Message);
                if (retries >= maxRetries)
                {
                    logger.LogError(ex, "Failed to connect to database after {MaxRetries} attempts.", maxRetries);
                    throw;
                }
                Thread.Sleep(2000);
            }
        }
    }
    else
    {
        db.Database.EnsureCreated();
    }
}

// Exception handling middleware
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseCors("AllowAll");

app.UseRouting();

app.MapControllers();

app.Run();

// Make Program accessible for WebApplicationFactory in integration tests
public partial class Program { }
