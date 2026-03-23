using Esatto.Outreach.Api.Endpoints;
using Esatto.Outreach.Infrastructure;
using Microsoft.OpenApi.Models;
using DotNetEnv;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.RateLimiting;

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


builder.Services.AddScoped<Esatto.Outreach.Application.UseCases.Auth.Register>();
builder.Services.AddScoped<Esatto.Outreach.Application.UseCases.Auth.Login>();
builder.Services.AddScoped<Esatto.Outreach.Application.UseCases.Auth.RefreshAccessToken>();
builder.Services.AddScoped<Esatto.Outreach.Application.UseCases.Auth.ValidateInvitation>();
builder.Services.AddScoped<Esatto.Outreach.Application.UseCases.Auth.AcceptInvitation>();
builder.Services.AddScoped<Esatto.Outreach.Application.UseCases.Auth.CreateInvitation>();
builder.Services.AddScoped<Esatto.Outreach.Application.UseCases.Prospects.CreateProspect>();
builder.Services.AddScoped<Esatto.Outreach.Application.UseCases.Prospects.UpdateProspect>();
builder.Services.AddScoped<Esatto.Outreach.Application.UseCases.Prospects.GetProspectById>();
builder.Services.AddScoped<Esatto.Outreach.Application.UseCases.Prospects.ListProspects>();
builder.Services.AddScoped<Esatto.Outreach.Application.UseCases.Prospects.DeleteProspect>();
builder.Services.AddScoped<Esatto.Outreach.Application.UseCases.Prospects.AddContactPerson>();
builder.Services.AddScoped<Esatto.Outreach.Application.UseCases.Prospects.UpdateContactPerson>();
builder.Services.AddScoped<Esatto.Outreach.Application.UseCases.Prospects.DeleteContactPerson>();
builder.Services.AddScoped<Esatto.Outreach.Application.UseCases.SoftDataCollection.EnrichContactPerson>();
builder.Services.AddScoped<Esatto.Outreach.Application.UseCases.Prospects.SetActiveContact>();
builder.Services.AddScoped<Esatto.Outreach.Application.UseCases.Prospects.ClearActiveContact>();
builder.Services.AddScoped<Esatto.Outreach.Application.UseCases.Prospects.GetActiveContact>();
builder.Services.AddScoped<Esatto.Outreach.Application.UseCases.EmailGeneration.GenerateMail>();
builder.Services.AddScoped<Esatto.Outreach.Application.UseCases.EmailGeneration.GenerateLinkedInMessage>();
builder.Services.AddScoped<Esatto.Outreach.Application.UseCases.Chat.ChatWithProspect>();
builder.Services.AddScoped<Esatto.Outreach.Application.UseCases.Chat.ResetProspectChat>();
builder.Services.AddScoped<Esatto.Outreach.Application.UseCases.SoftDataCollection.GenerateEntityIntelligence>();
builder.Services.AddScoped<Esatto.Outreach.Application.UseCases.OutreachPrompts.ListOutreachPrompts>();
builder.Services.AddScoped<Esatto.Outreach.Application.UseCases.OutreachPrompts.GetActiveOutreachPrompt>();
builder.Services.AddScoped<Esatto.Outreach.Application.UseCases.OutreachPrompts.CreateOutreachPrompt>();
builder.Services.AddScoped<Esatto.Outreach.Application.UseCases.OutreachPrompts.UpdateOutreachPrompt>();
builder.Services.AddScoped<Esatto.Outreach.Application.UseCases.OutreachPrompts.ActivateOutreachPrompt>();
builder.Services.AddScoped<Esatto.Outreach.Application.UseCases.OutreachPrompts.DeleteOutreachPrompt>();
builder.Services.AddScoped<Esatto.Outreach.Application.UseCases.CompanyInfo.GetCompanyInfo>();
builder.Services.AddScoped<Esatto.Outreach.Application.UseCases.CompanyInfo.UpdateCompanyInfo>();
builder.Services.AddScoped<Esatto.Outreach.Application.UseCases.ProjectCases.GetProjectCases>();
builder.Services.AddScoped<Esatto.Outreach.Application.UseCases.ProjectCases.GetProjectCase>();
builder.Services.AddScoped<Esatto.Outreach.Application.UseCases.ProjectCases.CreateProjectCase>();
builder.Services.AddScoped<Esatto.Outreach.Application.UseCases.ProjectCases.UpdateProjectCase>();
builder.Services.AddScoped<Esatto.Outreach.Application.UseCases.ProjectCases.DeleteProjectCase>();
builder.Services.AddScoped<Esatto.Outreach.Application.UseCases.CapsuleDataSource.CreateOrUpdateProspectFromCapsule>();
builder.Services.AddScoped<Esatto.Outreach.Application.UseCases.CapsuleDataSource.ClaimPendingProspect>();
builder.Services.AddScoped<Esatto.Outreach.Application.UseCases.CapsuleDataSource.RejectPendingProspect>();
builder.Services.AddScoped<Esatto.Outreach.Application.UseCases.CapsuleDataSource.ListPendingProspects>();
builder.Services.AddScoped<Esatto.Outreach.Application.UseCases.CapsuleDataSource.HandleCapsuleWebhook>();

// Workflow Worker
builder.Services.AddHostedService<Esatto.Outreach.Api.Workers.WorkflowExecutionWorker>();
builder.Services.AddHostedService<Esatto.Outreach.Api.Workers.WorkflowCleanupWorker>();


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
app.MapWorkflowEndpoints();


app.Run();

public partial class Program { }
