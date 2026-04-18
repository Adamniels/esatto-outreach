using Esatto.Outreach.Api.Endpoints;
using Esatto.Outreach.Application;
using Esatto.Outreach.Domain.Exceptions;
using Esatto.Outreach.Infrastructure;
using Microsoft.OpenApi;
using DotNetEnv;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;

// Load .env file only if it exists (local development)
// On Azure, Application Settings are used instead
var envPath = Path.Combine(Directory.GetCurrentDirectory(), "..", ".env");
if (File.Exists(envPath))
{
    Env.Load(envPath);
}

var builder = WebApplication.CreateBuilder(args);

// Add environment variables explicitly to configuration
// This ensures Azure App Service environment variables are loaded
builder.Configuration.AddEnvironmentVariables();

// Inject database password from environment variable into connection string
var dbPasswordEnvVar = builder.Configuration.GetSection("Database")["PasswordEnvVar"];
if (!string.IsNullOrWhiteSpace(dbPasswordEnvVar))
{
    var dbPassword = Environment.GetEnvironmentVariable(dbPasswordEnvVar);
    if (!string.IsNullOrWhiteSpace(dbPassword))
    {
        var connectionString = builder.Configuration.GetConnectionString("Default");
        if (!string.IsNullOrWhiteSpace(connectionString) && connectionString.Contains("{0}"))
        {
            builder.Configuration["ConnectionStrings:Default"] = string.Format(connectionString, dbPassword);
        }
    }
}

// Configure JSON options for case-insensitive property matching and String Enums
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNameCaseInsensitive = true;
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

// Infrastructure (DbContext + repository via our extension method)
builder.Services.AddInfrastructure(builder.Configuration);

// Use-cases (each action the system can perform)
builder.Services.AddApplication();

if (builder.Configuration.GetValue("BackgroundWorkers:Enabled", true))
{
    builder.Services.AddHostedService<Esatto.Outreach.Api.Workers.SequenceExecutionWorker>();
    builder.Services.AddHostedService<Esatto.Outreach.Api.Workers.SequenceThrottleWorker>();
}


// CORS - allow frontend UI
builder.Services.AddCors(opt =>
{
    opt.AddPolicy("ui", p => p
        .AllowAnyHeader().AllowAnyMethod()
        .WithOrigins(
            "http://localhost:5173",
            "http://localhost:3000",
            "https://gray-rock-0149ba903.6.azurestaticapps.net"
        )
        .AllowCredentials());
});

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Esatto.Outreach API", Version = "v1" });
});

// Rate Limiting for Auth
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("AuthPolicy", opt =>
    {
        opt.PermitLimit = 5;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 2;
    });
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

var app = builder.Build();

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var exception = context.Features.Get<IExceptionHandlerPathFeature>()?.Error;
        if (exception == null)
        {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            return;
        }

        var (statusCode, message) = exception switch
        {
            UnauthorizedAccessException => (StatusCodes.Status403Forbidden, exception.Message),
            AuthenticationFailedException => (StatusCodes.Status401Unauthorized, exception.Message),
            KeyNotFoundException => (StatusCodes.Status404NotFound, exception.Message),
            ArgumentException => (StatusCodes.Status400BadRequest, exception.Message),
            InvalidOperationException => (StatusCodes.Status400BadRequest, exception.Message),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred.")
        };

        context.Response.StatusCode = statusCode;
        await context.Response.WriteAsJsonAsync(new { error = message });
    });
});

// Configure port - use Azure's PORT environment variable if available, otherwise default to 3000
var port = Environment.GetEnvironmentVariable("PORT") ?? "3000";
app.Urls.Add($"http://0.0.0.0:{port}");

app.UseCors("ui");

// ============ AUTHENTICATION & AUTHORIZATION MIDDLEWARE ============
app.UseAuthentication();  // MUST be before UseAuthorization
app.UseAuthorization();
app.UseRateLimiter();
// ===================================================================

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Health check endpoint
app.MapGet("/healthz", () => Results.Ok(new { status = "ok" }));

// ============ MAP ALL ENDPOINTS ============
app.MapAuthEndpoints();
app.MapInvitationEndpoints();
app.MapProspectEndpoints();
app.MapCapsuleEndpoints();
app.MapOutreachPromptEndpoints();
app.MapCompanyInfoEndpoints();
app.MapSequenceEndpoints();

// Keep local/dev startup resilient by creating/updating schema automatically.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<OutreachDbContext>();
    await db.Database.MigrateAsync();
}

app.Run();

public partial class Program { }
