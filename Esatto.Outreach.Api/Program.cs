using Esatto.Outreach.Api.Endpoints;
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


builder.Services.AddScoped<Esatto.Outreach.Application.Features.Auth.RegisterUser.RegisterCommandHandler>();
builder.Services.AddScoped<Esatto.Outreach.Application.Features.Auth.LoginUser.LoginCommandHandler>();
builder.Services.AddScoped<Esatto.Outreach.Application.Features.Auth.RefreshToken.RefreshAccessTokenCommandHandler>();
builder.Services.AddScoped<Esatto.Outreach.Application.Features.Auth.ValidateInvitation.ValidateInvitationCommandHandler>();
builder.Services.AddScoped<Esatto.Outreach.Application.Features.Auth.AcceptInvitation.AcceptInvitationCommandHandler>();
builder.Services.AddScoped<Esatto.Outreach.Application.Features.Auth.InviteUser.InviteUserCommandHandler>();
builder.Services.AddScoped<Esatto.Outreach.Application.Features.Prospects.CreateProspect.CreateProspectCommandHandler>();
builder.Services.AddScoped<Esatto.Outreach.Application.Features.Prospects.UpdateProspect.UpdateProspectCommandHandler>();
builder.Services.AddScoped<Esatto.Outreach.Application.Features.Prospects.GetProspectById.GetProspectByIdQueryHandler>();
builder.Services.AddScoped<Esatto.Outreach.Application.Features.Prospects.ListProspects.ListProspectsQueryHandler>();
builder.Services.AddScoped<Esatto.Outreach.Application.Features.Prospects.DeleteProspect.DeleteProspectCommandHandler>();
builder.Services.AddScoped<Esatto.Outreach.Application.Features.Prospects.AddContactPerson.AddContactPersonCommandHandler>();
builder.Services.AddScoped<Esatto.Outreach.Application.Features.Prospects.UpdateContactPerson.UpdateContactPersonCommandHandler>();
builder.Services.AddScoped<Esatto.Outreach.Application.Features.Prospects.DeleteContactPerson.DeleteContactPersonCommandHandler>();
builder.Services.AddScoped<Esatto.Outreach.Application.Features.Intelligence.EnrichContactPerson.EnrichContactPersonCommandHandler>();
builder.Services.AddScoped<Esatto.Outreach.Application.Features.Prospects.SetActiveContact.SetActiveContactCommandHandler>();
builder.Services.AddScoped<Esatto.Outreach.Application.Features.Prospects.ClearActiveContact.ClearActiveContactCommandHandler>();
builder.Services.AddScoped<Esatto.Outreach.Application.Features.Prospects.GetActiveContact.GetActiveContactQueryHandler>();
builder.Services.AddScoped<Esatto.Outreach.Application.Features.OutreachGeneration.GenerateEmailDraft.GenerateMailCommandHandler>();
builder.Services.AddScoped<Esatto.Outreach.Application.Features.OutreachGeneration.GenerateLinkedInDraft.GenerateLinkedInMessageCommandHandler>();
builder.Services.AddScoped<Esatto.Outreach.Application.Features.Intelligence.ChatWithProspect.ChatWithProspectCommandHandler>();
builder.Services.AddScoped<Esatto.Outreach.Application.Features.Intelligence.ResetProspectChat.ResetProspectChatCommandHandler>();
builder.Services.AddScoped<Esatto.Outreach.Application.Features.Intelligence.GenerateEntityIntelligence.GenerateEntityIntelligenceCommandHandler>();
builder.Services.AddScoped<Esatto.Outreach.Application.Features.OutreachPrompts.ListOutreachPrompts.ListOutreachPromptsQueryHandler>();
builder.Services.AddScoped<Esatto.Outreach.Application.Features.OutreachPrompts.GetActiveOutreachPrompt.GetActiveOutreachPromptQueryHandler>();
builder.Services.AddScoped<Esatto.Outreach.Application.Features.OutreachPrompts.CreateOutreachPrompt.CreateOutreachPromptCommandHandler>();
builder.Services.AddScoped<Esatto.Outreach.Application.Features.OutreachPrompts.UpdateOutreachPrompt.UpdateOutreachPromptCommandHandler>();
builder.Services.AddScoped<Esatto.Outreach.Application.Features.OutreachPrompts.ActivateOutreachPrompt.ActivateOutreachPromptCommandHandler>();
builder.Services.AddScoped<Esatto.Outreach.Application.Features.OutreachPrompts.DeleteOutreachPrompt.DeleteOutreachPromptCommandHandler>();
builder.Services.AddScoped<Esatto.Outreach.Application.Features.Intelligence.GetCompanyInfo.GetCompanyInfoQueryHandler>();
builder.Services.AddScoped<Esatto.Outreach.Application.Features.Intelligence.UpdateCompanyInfo.UpdateCompanyInfoCommandHandler>();
builder.Services.AddScoped<Esatto.Outreach.Application.Features.ProjectCases.ListProjectCases.ListProjectCasesQueryHandler>();
builder.Services.AddScoped<Esatto.Outreach.Application.Features.ProjectCases.GetProjectCase.GetProjectCaseQueryHandler>();
builder.Services.AddScoped<Esatto.Outreach.Application.Features.ProjectCases.CreateProjectCase.CreateProjectCaseCommandHandler>();
builder.Services.AddScoped<Esatto.Outreach.Application.Features.ProjectCases.UpdateProjectCase.UpdateProjectCaseCommandHandler>();
builder.Services.AddScoped<Esatto.Outreach.Application.Features.ProjectCases.DeleteProjectCase.DeleteProjectCaseCommandHandler>();
builder.Services.AddScoped<Esatto.Outreach.Application.Features.Webhooks.CreateOrUpdateProspectFromCapsule.CreateOrUpdateProspectFromCapsuleCommandHandler>();
builder.Services.AddScoped<Esatto.Outreach.Application.Features.Webhooks.ClaimPendingProspect.ClaimPendingProspectCommandHandler>();
builder.Services.AddScoped<Esatto.Outreach.Application.Features.Webhooks.RejectPendingProspect.RejectPendingProspectCommandHandler>();
builder.Services.AddScoped<Esatto.Outreach.Application.Features.Webhooks.ListPendingProspects.ListPendingProspectsQueryHandler>();
builder.Services.AddScoped<Esatto.Outreach.Application.Features.Webhooks.HandleCapsuleWebhook.HandleCapsuleWebhookCommandHandler>();

builder.Services.AddScoped<Esatto.Outreach.Application.Features.Sequences.Shared.SequenceAccessCommandHandler>();
builder.Services.AddScoped<Esatto.Outreach.Application.Features.Sequences.CreateSequence.CreateSequenceCommandHandler>();
builder.Services.AddScoped<Esatto.Outreach.Application.Features.Sequences.UpdateSequence.UpdateSequenceCommandHandler>();
builder.Services.AddScoped<Esatto.Outreach.Application.Features.Sequences.GetSequence.GetSequenceQueryHandler>();
builder.Services.AddScoped<Esatto.Outreach.Application.Features.Sequences.DeleteSequence.DeleteSequenceCommandHandler>();
builder.Services.AddScoped<Esatto.Outreach.Application.Features.Sequences.ListSequences.ListSequencesQueryHandler>();
builder.Services.AddScoped<Esatto.Outreach.Application.Features.Sequences.AddSequenceStep.AddSequenceStepCommandHandler>();
builder.Services.AddScoped<Esatto.Outreach.Application.Features.Sequences.UpdateSequenceStep.UpdateSequenceStepCommandHandler>();
builder.Services.AddScoped<Esatto.Outreach.Application.Features.Sequences.UpdateSequenceStepContent.UpdateSequenceStepContentCommandHandler>();
builder.Services.AddScoped<Esatto.Outreach.Application.Features.Sequences.DeleteSequenceStep.DeleteSequenceStepCommandHandler>();
builder.Services.AddScoped<Esatto.Outreach.Application.Features.Sequences.ReorderSequenceSteps.ReorderSequenceStepsCommandHandler>();
builder.Services.AddScoped<Esatto.Outreach.Application.Features.Sequences.EnrollProspect.EnrollProspectCommandHandler>();
builder.Services.AddScoped<Esatto.Outreach.Application.Features.Sequences.RemoveProspect.RemoveProspectCommandHandler>();
builder.Services.AddScoped<Esatto.Outreach.Application.Features.Sequences.ActivateSequence.ActivateSequenceCommandHandler>();
builder.Services.AddScoped<Esatto.Outreach.Application.Features.Sequences.PauseSequence.PauseSequenceCommandHandler>();
builder.Services.AddScoped<Esatto.Outreach.Application.Features.Sequences.CancelSequence.CancelSequenceCommandHandler>();
builder.Services.AddScoped<Esatto.Outreach.Application.Features.Sequences.GenerateStepContent.GenerateStepContentCommandHandler>();
builder.Services.AddScoped<Esatto.Outreach.Application.Features.Sequences.SequenceOrchestrator.SequenceOrchestratorCommandHandler>();
builder.Services.AddScoped<Esatto.Outreach.Application.Features.Sequences.SaveBuilderProgress.SaveBuilderProgressCommandHandler>();
builder.Services.AddScoped<Esatto.Outreach.Application.Features.Sequences.CompleteSequenceSetup.CompleteSequenceSetupCommandHandler>();

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
