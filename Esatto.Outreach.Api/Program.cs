using Esatto.Outreach.Api.Endpoints;
using Esatto.Outreach.Infrastructure;
using Microsoft.OpenApi.Models;
using DotNetEnv;

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

// Configure JSON options for case-insensitive property matching
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNameCaseInsensitive = true;
});

// Infrastructure (DbContext + repository via our extension method)
builder.Services.AddInfrastructure(builder.Configuration);

// Use-cases (each action the system can perform)
builder.Services.AddScoped<Esatto.Outreach.Application.UseCases.Auth.Register>();
builder.Services.AddScoped<Esatto.Outreach.Application.UseCases.Auth.Login>();
builder.Services.AddScoped<Esatto.Outreach.Application.UseCases.Auth.RefreshAccessToken>();
builder.Services.AddScoped<Esatto.Outreach.Application.UseCases.Prospects.CreateProspect>();
builder.Services.AddScoped<Esatto.Outreach.Application.UseCases.Prospects.UpdateProspect>();
builder.Services.AddScoped<Esatto.Outreach.Application.UseCases.Prospects.GetProspectById>();
builder.Services.AddScoped<Esatto.Outreach.Application.UseCases.Prospects.ListProspects>();
builder.Services.AddScoped<Esatto.Outreach.Application.UseCases.Prospects.DeleteProspect>();
builder.Services.AddScoped<Esatto.Outreach.Application.UseCases.Prospects.AddContactPerson>();
builder.Services.AddScoped<Esatto.Outreach.Application.UseCases.Prospects.UpdateContactPerson>();
builder.Services.AddScoped<Esatto.Outreach.Application.UseCases.Prospects.DeleteContactPerson>();
builder.Services.AddScoped<Esatto.Outreach.Application.UseCases.SoftDataCollection.EnrichContactPerson>();
builder.Services.AddScoped<Esatto.Outreach.Application.UseCases.EmailGeneration.GenerateMailOpenAIResponeAPI>();
builder.Services.AddScoped<Esatto.Outreach.Application.UseCases.EmailDelivery.SendEmailViaN8n>();
builder.Services.AddScoped<Esatto.Outreach.Application.UseCases.Chat.ChatWithProspect>();
builder.Services.AddScoped<Esatto.Outreach.Application.UseCases.Chat.ResetProspectChat>();
builder.Services.AddScoped<Esatto.Outreach.Application.UseCases.SoftDataCollection.GenerateEntityIntelligence>();
builder.Services.AddScoped<Esatto.Outreach.Application.UseCases.EmailPrompts.ListEmailPrompts>();
builder.Services.AddScoped<Esatto.Outreach.Application.UseCases.EmailPrompts.GetActiveEmailPrompt>();
builder.Services.AddScoped<Esatto.Outreach.Application.UseCases.EmailPrompts.CreateEmailPrompt>();
builder.Services.AddScoped<Esatto.Outreach.Application.UseCases.EmailPrompts.UpdateEmailPrompt>();
builder.Services.AddScoped<Esatto.Outreach.Application.UseCases.EmailPrompts.ActivateEmailPrompt>();
builder.Services.AddScoped<Esatto.Outreach.Application.UseCases.EmailPrompts.DeleteEmailPrompt>();
builder.Services.AddScoped<Esatto.Outreach.Application.UseCases.Batch.GenerateEntityIntelligenceBatch>();
builder.Services.AddScoped<Esatto.Outreach.Application.UseCases.Batch.GenerateEmailBatch>();
builder.Services.AddScoped<Esatto.Outreach.Application.UseCases.CompanyInfo.GetCompanyInfo>();
builder.Services.AddScoped<Esatto.Outreach.Application.UseCases.CapsuleDataSource.CreateOrUpdateProspectFromCapsule>();
builder.Services.AddScoped<Esatto.Outreach.Application.UseCases.CapsuleDataSource.ClaimPendingProspect>();
builder.Services.AddScoped<Esatto.Outreach.Application.UseCases.CapsuleDataSource.RejectPendingProspect>();
builder.Services.AddScoped<Esatto.Outreach.Application.UseCases.CapsuleDataSource.ListPendingProspects>();
builder.Services.AddScoped<Esatto.Outreach.Application.UseCases.CapsuleDataSource.HandleCapsuleWebhook>();

// CORS - allow n8n and frontend UI
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

var app = builder.Build();

// Configure port - use Azure's PORT environment variable if available, otherwise default to 3000
var port = Environment.GetEnvironmentVariable("PORT") ?? "3000";
app.Urls.Add($"http://0.0.0.0:{port}");

app.UseCors("ui");

// ============ AUTHENTICATION & AUTHORIZATION MIDDLEWARE ============
app.UseAuthentication();  // MUST be before UseAuthorization
app.UseAuthorization();
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
app.MapProspectEndpoints();
app.MapBatchEndpoints();
app.MapCapsuleEndpoints();
app.MapEmailPromptEndpoints();
app.MapCompanyInfoEndpoints();

app.Run();
