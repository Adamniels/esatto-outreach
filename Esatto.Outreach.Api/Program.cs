using Esatto.Outreach.Api.Endpoints;
using Esatto.Outreach.Infrastructure;
using Microsoft.OpenApi;
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


builder.Services.AddScoped<Esatto.Outreach.Application.Features.Auth.RegisterCommandHandler>();
builder.Services.AddScoped<Esatto.Outreach.Application.Features.Auth.LoginCommandHandler>();
builder.Services.AddScoped<Esatto.Outreach.Application.Features.Auth.RefreshAccessTokenCommandHandler>();
builder.Services.AddScoped<Esatto.Outreach.Application.Features.Auth.ValidateInvitationCommandHandler>();
builder.Services.AddScoped<Esatto.Outreach.Application.Features.Auth.AcceptInvitationCommandHandler>();
builder.Services.AddScoped<Esatto.Outreach.Application.Features.Auth.CreateInvitationCommandHandler>();
builder.Services.AddScoped<Esatto.Outreach.Application.Features.Prospects.CreateProspectCommandHandler>();
builder.Services.AddScoped<Esatto.Outreach.Application.Features.Prospects.UpdateProspectCommandHandler>();
builder.Services.AddScoped<Esatto.Outreach.Application.Features.Prospects.GetProspectByIdQueryHandler>();
builder.Services.AddScoped<Esatto.Outreach.Application.Features.Prospects.ListProspectsQueryHandler>();
builder.Services.AddScoped<Esatto.Outreach.Application.Features.Prospects.DeleteProspectCommandHandler>();
builder.Services.AddScoped<Esatto.Outreach.Application.Features.Prospects.AddContactPersonCommandHandler>();
builder.Services.AddScoped<Esatto.Outreach.Application.Features.Prospects.UpdateContactPersonCommandHandler>();
builder.Services.AddScoped<Esatto.Outreach.Application.Features.Prospects.DeleteContactPersonCommandHandler>();
builder.Services.AddScoped<Esatto.Outreach.Application.Features.Intelligence.EnrichContactPersonCommandHandler>();
builder.Services.AddScoped<Esatto.Outreach.Application.Features.Prospects.SetActiveContactCommandHandler>();
builder.Services.AddScoped<Esatto.Outreach.Application.Features.Prospects.ClearActiveContactCommandHandler>();
builder.Services.AddScoped<Esatto.Outreach.Application.Features.Prospects.GetActiveContactQueryHandler>();
builder.Services.AddScoped<Esatto.Outreach.Application.Features.OutreachGeneration.GenerateMailCommandHandler>();
builder.Services.AddScoped<Esatto.Outreach.Application.Features.OutreachGeneration.GenerateLinkedInMessageCommandHandler>();
builder.Services.AddScoped<Esatto.Outreach.Application.Features.Intelligence.ChatWithProspectCommandHandler>();
builder.Services.AddScoped<Esatto.Outreach.Application.Features.Intelligence.ResetProspectChatCommandHandler>();
builder.Services.AddScoped<Esatto.Outreach.Application.Features.Intelligence.GenerateEntityIntelligenceCommandHandler>();
builder.Services.AddScoped<Esatto.Outreach.Application.Features.OutreachPrompts.ListOutreachPromptsQueryHandler>();
builder.Services.AddScoped<Esatto.Outreach.Application.Features.OutreachPrompts.GetActiveOutreachPromptQueryHandler>();
builder.Services.AddScoped<Esatto.Outreach.Application.Features.OutreachPrompts.CreateOutreachPromptCommandHandler>();
builder.Services.AddScoped<Esatto.Outreach.Application.Features.OutreachPrompts.UpdateOutreachPromptCommandHandler>();
builder.Services.AddScoped<Esatto.Outreach.Application.Features.OutreachPrompts.ActivateOutreachPromptCommandHandler>();
builder.Services.AddScoped<Esatto.Outreach.Application.Features.OutreachPrompts.DeleteOutreachPromptCommandHandler>();
builder.Services.AddScoped<Esatto.Outreach.Application.Features.Intelligence.GetCompanyInfoQueryHandler>();
builder.Services.AddScoped<Esatto.Outreach.Application.Features.Intelligence.UpdateCompanyInfoCommandHandler>();
builder.Services.AddScoped<Esatto.Outreach.Application.Features.ProjectCases.GetProjectCasesQueryHandler>();
builder.Services.AddScoped<Esatto.Outreach.Application.Features.ProjectCases.GetProjectCaseQueryHandler>();
builder.Services.AddScoped<Esatto.Outreach.Application.Features.ProjectCases.CreateProjectCaseCommandHandler>();
builder.Services.AddScoped<Esatto.Outreach.Application.Features.ProjectCases.UpdateProjectCaseCommandHandler>();
builder.Services.AddScoped<Esatto.Outreach.Application.Features.ProjectCases.DeleteProjectCaseCommandHandler>();
builder.Services.AddScoped<Esatto.Outreach.Application.Features.Webhooks.CreateOrUpdateProspectFromCapsuleCommandHandler>();
builder.Services.AddScoped<Esatto.Outreach.Application.Features.Webhooks.ClaimPendingProspectCommandHandler>();
builder.Services.AddScoped<Esatto.Outreach.Application.Features.Webhooks.RejectPendingProspectCommandHandler>();
builder.Services.AddScoped<Esatto.Outreach.Application.Features.Webhooks.ListPendingProspectsQueryHandler>();
builder.Services.AddScoped<Esatto.Outreach.Application.Features.Webhooks.HandleCapsuleWebhookCommandHandler>();

builder.Services.AddScoped<Esatto.Outreach.Application.Features.Sequences.SequenceAccessCommandHandler>();
builder.Services.AddScoped<Esatto.Outreach.Application.Features.Sequences.CreateSequenceCommandHandler>();
builder.Services.AddScoped<Esatto.Outreach.Application.Features.Sequences.UpdateSequenceCommandHandler>();
builder.Services.AddScoped<Esatto.Outreach.Application.Features.Sequences.GetSequenceQueryHandler>();
builder.Services.AddScoped<Esatto.Outreach.Application.Features.Sequences.DeleteSequenceCommandHandler>();
builder.Services.AddScoped<Esatto.Outreach.Application.Features.Sequences.ListSequencesQueryHandler>();
builder.Services.AddScoped<Esatto.Outreach.Application.Features.Sequences.AddSequenceStepCommandHandler>();
builder.Services.AddScoped<Esatto.Outreach.Application.Features.Sequences.UpdateSequenceStepCommandHandler>();
builder.Services.AddScoped<Esatto.Outreach.Application.Features.Sequences.UpdateSequenceStepContentCommandHandler>();
builder.Services.AddScoped<Esatto.Outreach.Application.Features.Sequences.DeleteSequenceStepCommandHandler>();
builder.Services.AddScoped<Esatto.Outreach.Application.Features.Sequences.ReorderSequenceStepsCommandHandler>();
builder.Services.AddScoped<Esatto.Outreach.Application.Features.Sequences.EnrollProspectCommandHandler>();
builder.Services.AddScoped<Esatto.Outreach.Application.Features.Sequences.RemoveProspectCommandHandler>();
builder.Services.AddScoped<Esatto.Outreach.Application.Features.Sequences.ActivateSequenceCommandHandler>();
builder.Services.AddScoped<Esatto.Outreach.Application.Features.Sequences.PauseSequenceCommandHandler>();
builder.Services.AddScoped<Esatto.Outreach.Application.Features.Sequences.CancelSequenceCommandHandler>();
builder.Services.AddScoped<Esatto.Outreach.Application.Features.Sequences.GenerateStepContentCommandHandler>();
builder.Services.AddScoped<Esatto.Outreach.Application.Features.Sequences.SequenceOrchestratorCommandHandler>();
builder.Services.AddScoped<Esatto.Outreach.Application.Features.Sequences.SaveBuilderProgressCommandHandler>();
builder.Services.AddScoped<Esatto.Outreach.Application.Features.Sequences.CompleteSequenceSetupCommandHandler>();

builder.Services.AddHostedService<Esatto.Outreach.Api.Workers.SequenceExecutionWorker>();
builder.Services.AddHostedService<Esatto.Outreach.Api.Workers.SequenceThrottleWorker>();


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
app.MapSequenceEndpoints();

app.Run();

public partial class Program { }
