using Esatto.Outreach.Application.Abstractions;
using Esatto.Outreach.Application.DTOs;
using Esatto.Outreach.Application.DTOs.Auth;
using Esatto.Outreach.Application.UseCases.Auth;
using Esatto.Outreach.Application.UseCases.Prospects;
using Esatto.Outreach.Application.UseCases.EmailPrompts;
using Esatto.Outreach.Application.UseCases.EmailGeneration;
using Esatto.Outreach.Application.UseCases.EmailDelivery;
using Esatto.Outreach.Application.UseCases.SoftDataCollection;
using Esatto.Outreach.Application.UseCases.Chat;
using Esatto.Outreach.Application.UseCases.CompanyInfo;
using Esatto.Outreach.Application.UseCases.Batch;
using Esatto.Outreach.Application.UseCases.CapsuleDataSource;
using Esatto.Outreach.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using DotNetEnv;
using System.Security.Claims;

// Läs .env filen endast om den finns (lokalt)
// På Azure används Application Settings istället
var envPath = Path.Combine(Directory.GetCurrentDirectory(), "..", ".env");
if (File.Exists(envPath))
{
    Env.Load(envPath);
}

var builder = WebApplication.CreateBuilder(args);

// Lägg till miljövariabler explicit i konfigurationen
// Detta säkerställer att Azure App Service environment variables läses in
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

// Infrastructure (DbContext + repo via vår helper)
builder.Services.AddInfrastructure(builder.Configuration);

// Use-cases (enkelt att börja så här)
// en handling som systemet kan utföra
// Auth use cases
builder.Services.AddScoped<Register>();
builder.Services.AddScoped<Login>();
builder.Services.AddScoped<RefreshAccessToken>();

// Prospect use cases
builder.Services.AddScoped<CreateProspect>();
builder.Services.AddScoped<UpdateProspect>();
builder.Services.AddScoped<GetProspectById>();
builder.Services.AddScoped<GetAllProspects>();
builder.Services.AddScoped<ListProspects>();
builder.Services.AddScoped<DeleteProspect>();
builder.Services.AddScoped<GenerateMailOpenAIResponeAPI>();
builder.Services.AddScoped<SendEmailViaN8n>();
builder.Services.AddScoped<ChatWithProspect>();
builder.Services.AddScoped<ResetProspectChat>();
builder.Services.AddScoped<GenerateSoftCompanyData>();
builder.Services.AddScoped<ListEmailPrompts>();
builder.Services.AddScoped<GetActiveEmailPrompt>();
builder.Services.AddScoped<CreateEmailPrompt>();
builder.Services.AddScoped<UpdateEmailPrompt>();
builder.Services.AddScoped<ActivateEmailPrompt>();
builder.Services.AddScoped<DeleteEmailPrompt>();

// Batch use cases
builder.Services.AddScoped<GenerateSoftDataBatch>();
builder.Services.AddScoped<GenerateEmailBatch>();

// Company Info use cases
builder.Services.AddScoped<GetCompanyInfo>();

// Capsule DataSource use cases
builder.Services.AddScoped<CreateOrUpdateProspectFromCapsule>();
builder.Services.AddScoped<ClaimPendingProspect>();
builder.Services.AddScoped<RejectPendingProspect>();
builder.Services.AddScoped<ListPendingProspects>();
builder.Services.AddScoped<HandleCapsuleWebhook>();

// CORS – tillåt n8n/valfritt UI
builder.Services.AddCors(opt =>
{
    opt.AddPolicy("ui", p => p
        .AllowAnyHeader().AllowAnyMethod()
        .WithOrigins("http://localhost:5173", "http://localhost:3000"));
});

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Esatto.Outreach API", Version = "v1" });
});

var app = builder.Build();

// Konfigurera att köra på port 3000
app.Urls.Add("http://localhost:3000");

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

app.MapGet("/healthz", () => Results.Ok(new { status = "ok" }));

// ============ AUTH ENDPOINTS ============
var auth = app.MapGroup("/auth").WithTags("Authentication");

auth.MapPost("/register", async (
    RegisterRequestDto dto,
    Register useCase,
    CancellationToken ct) =>
{
    var (success, data, error) = await useCase.Handle(dto, ct);
    return success
        ? Results.Ok(data)
        : Results.BadRequest(new { error });
});

auth.MapPost("/login", async (
    LoginRequestDto dto,
    Login useCase,
    CancellationToken ct) =>
{
    var (success, data, error) = await useCase.Handle(dto, ct);
    return success
        ? Results.Ok(data)
        : Results.BadRequest(new { error });
});

auth.MapPost("/refresh", async (
    RefreshTokenRequestDto dto,
    RefreshAccessToken useCase,
    CancellationToken ct) =>
{
    var (success, data, error) = await useCase.Handle(dto, ct);
    return success
        ? Results.Ok(data)
        : Results.Unauthorized();
});

// ============ PROTECTED PROSPECTS ENDPOINTS ============

// Här injiceras usecaset från ovan
app.MapGet("/prospects", async (ListProspects useCase, ClaimsPrincipal user, CancellationToken ct) =>
{
    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
    if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();
    var list = await useCase.Handle(userId, ct);
    return Results.Ok(list);
})
.RequireAuthorization();

app.MapGet("/prospects/{id:guid}", async (Guid id, GetProspectById useCase, CancellationToken ct) =>
{
    var dto = await useCase.Handle(id, ct);
    return dto is null ? Results.NotFound() : Results.Ok(dto);
})
.RequireAuthorization();

app.MapPost("/prospects", async (ProspectCreateDto dto, CreateProspect useCase, ClaimsPrincipal user, CancellationToken ct) =>
{
    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
    if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();
    if (string.IsNullOrWhiteSpace(dto.Name))
        return Results.BadRequest(new { error = "Name is required" });

    var created = await useCase.Handle(dto, userId, ct);
    return Results.Created($"/prospects/{created.Id}", created);
})
.RequireAuthorization();

app.MapPut("/prospects/{id:guid}", async (Guid id, ProspectUpdateDto dto, UpdateProspect useCase, ClaimsPrincipal user, CancellationToken ct) =>
{
    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
    if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();
    try
    {
        var updated = await useCase.Handle(id, dto, userId, ct);
        return updated is null ? Results.NotFound() : Results.Ok(updated);
    }
    catch (UnauthorizedAccessException)
    {
        return Results.StatusCode(403); // Forbidden
    }
})
.RequireAuthorization();

app.MapDelete("/prospects/{id:guid}", async (Guid id, DeleteProspect useCase, ClaimsPrincipal user, CancellationToken ct) =>
{
    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
    if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();
    
    try
    {
        var deleted = await useCase.ExecuteAsync(id, userId, ct);
        return deleted ? Results.NoContent() : Results.NotFound();
    }
    catch (UnauthorizedAccessException)
    {
        return Results.StatusCode(403); // Forbidden
    }
})
.RequireAuthorization();

// --- Prospects endpoints (via use-cases) ---


app.MapPost("/prospects/{id:guid}/email/draft", async (
   Guid id,
   GenerateMailOpenAIResponeAPI useCase,
   ClaimsPrincipal user,
   string? type,
   CancellationToken ct) =>
{
    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
    if (string.IsNullOrEmpty(userId))
        return Results.Unauthorized();

    try
    {
        var prospect = await useCase.Handle(id, userId, type, ct);
        return Results.Ok(prospect);
    }
    catch (InvalidOperationException ex)
    {
        // e.g. prospect not found, no collected data
        return Results.NotFound(new { error = ex.Message });
    }
    catch (ArgumentException ex)
    {
        // e.g. invalid type parameter
        return Results.BadRequest(new { error = ex.Message });
    }
}).RequireAuthorization();

app.MapPost("/prospects/{id:guid}/email/send", async (
    Guid id,
    SendEmailViaN8n useCase,
    CancellationToken ct) =>
{
    try
    {
        var result = await useCase.Handle(id, ct);
        return result.Success
            ? Results.Ok(result)
            : Results.BadRequest(result);
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
    catch (Exception ex)
    {
        return Results.Problem(
            detail: ex.Message,
            statusCode: 500);
    }
});

app.MapPost("/prospects/{id:guid}/chat", async (Guid id, ChatRequestDto dto, ChatWithProspect useCase, CancellationToken ct) =>
{
    try
    {
        var res = await useCase.Handle(id, dto, ct);
        return Results.Ok(res);
    }
    catch (InvalidOperationException ex)
    {
        return Results.NotFound(new { error = ex.Message });
    }
});

app.MapPost("/prospects/{id:guid}/chat/reset", async (Guid id, ResetProspectChat useCase, CancellationToken ct) =>
{
    var success = await useCase.ExecuteAsync(id, ct);
    return success ? Results.NoContent() : Results.NotFound();
})
.RequireAuthorization();

app.MapPost("/prospects/{id:guid}/soft-data/generate", async (
    Guid id,
    [FromQuery] string? provider,
    GenerateSoftCompanyData useCase,
    CancellationToken ct) =>
{
    try
    {
        var softData = await useCase.Handle(id, provider, ct);
        return Results.Ok(softData);
    }
    catch (KeyNotFoundException ex)
    {
        return Results.NotFound(new { error = ex.Message });
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
    catch (Exception ex)
    {
        return Results.Problem(
            detail: ex.Message,
            statusCode: 500);
    }
});

// ============ BATCH ENDPOINTS ============

app.MapPost("/prospects/batch/soft-data/generate", async (
    BatchSoftDataRequest request,
    GenerateSoftDataBatch useCase,
    ClaimsPrincipal user,
    CancellationToken ct) =>
{
    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
    if (string.IsNullOrEmpty(userId))
        return Results.Unauthorized();

    try
    {
        var results = await useCase.Handle(request.ProspectIds, userId, request.Provider, ct);
        return Results.Ok(results);
    }
    catch (UnauthorizedAccessException)
    {
        return Results.StatusCode(403); // Forbidden
    }
    catch (Exception ex)
    {
        return Results.Problem(
            detail: ex.Message,
            statusCode: 500);
    }
})
.RequireAuthorization();

app.MapPost("/prospects/batch/email/generate", async (
    BatchEmailRequest request,
    GenerateEmailBatch useCase,
    ClaimsPrincipal user,
    CancellationToken ct) =>
{
    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
    if (string.IsNullOrEmpty(userId))
        return Results.Unauthorized();

    try
    {
        var results = await useCase.Handle(
            request.ProspectIds,
            userId,
            request.Type,
            request.AutoGenerateSoftData,
            request.SoftDataProvider,
            ct);
        return Results.Ok(results);
    }
    catch (UnauthorizedAccessException)
    {
        return Results.StatusCode(403); // Forbidden
    }
    catch (Exception ex)
    {
        return Results.Problem(
            detail: ex.Message,
            statusCode: 500);
    }
})
.RequireAuthorization();


// ============ CAPSULE CRM INTEGRATION ENDPOINTS ============

// Webhook endpoint for Capsule CRM (public, no auth)
app.MapPost("/webhooks/capsule", async (
    HttpContext httpContext,
    HandleCapsuleWebhook useCase,
    ILogger<Program> logger,
    CancellationToken ct) =>
{
    try
    {
        // Läs RAW body för debugging
        httpContext.Request.EnableBuffering();
        using var reader = new StreamReader(httpContext.Request.Body, leaveOpen: true);
        var rawBody = await reader.ReadToEndAsync(ct);
        httpContext.Request.Body.Position = 0;
        
        logger.LogInformation("=== CAPSULE WEBHOOK RECEIVED ===");
        logger.LogInformation("Raw Body: {RawBody}", rawBody);
        logger.LogInformation("Content-Type: {ContentType}", httpContext.Request.ContentType);
        
        // Försök deserialisera
        CapsuleWebhookEventDto? payload;
        try
        {
            payload = await httpContext.Request.ReadFromJsonAsync<CapsuleWebhookEventDto>(ct);
            logger.LogInformation("Successfully deserialized payload. Event: {Event}, Payload count: {Count}", 
                payload?.Type, payload?.Payload?.Count);
        }
        catch (Exception deserEx)
        {
            logger.LogError(deserEx, "Failed to deserialize webhook payload");
            return Results.BadRequest(new { error = "Invalid JSON format", details = deserEx.Message });
        }
        
        if (payload == null)
        {
            logger.LogWarning("Payload is null after deserialization");
            return Results.BadRequest(new { error = "Payload is required" });
        }
        
        var result = await useCase.Handle(payload, ct);
        logger.LogInformation("Webhook processed. Success: {Success}, Message: {Message}", 
            result.Success, result.Message);
        
        return result.Success 
            ? Results.Ok(result) 
            : Results.BadRequest(result);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Unhandled exception in Capsule webhook endpoint");
        return Results.Problem(
            detail: ex.Message,
            statusCode: 500,
            title: "Failed to process Capsule webhook");
    }
});

// List pending prospects (from Capsule, not yet claimed)
app.MapGet("/prospects/pending", async (
    ListPendingProspects useCase,
    CancellationToken ct) =>
{
    try
    {
        var pending = await useCase.Handle(ct);
        return Results.Ok(pending);
    }
    catch (Exception ex)
    {
        return Results.Problem(
            detail: ex.Message,
            statusCode: 500);
    }
})
.RequireAuthorization();

// Claim a pending prospect
app.MapPost("/prospects/{id:guid}/claim", async (
    Guid id,
    ClaimPendingProspect useCase,
    ClaimsPrincipal user,
    CancellationToken ct) =>
{
    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
    if (string.IsNullOrEmpty(userId))
        return Results.Unauthorized();

    try
    {
        var claimed = await useCase.Handle(id, userId, ct);
        return claimed == null ? Results.NotFound() : Results.Ok(claimed);
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
})
.RequireAuthorization();

// Reject a pending prospect (delete it)
app.MapPost("/prospects/{id:guid}/pending/reject", async (
    Guid id,
    RejectPendingProspect useCase,
    CancellationToken ct) =>
{
    try
    {
        var deleted = await useCase.Handle(id, ct);
        return deleted ? Results.NoContent() : Results.NotFound();
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
})
.RequireAuthorization();


// --- Email Prompt Settings endpoints ---

// Get active prompt
app.MapGet("/settings/email-prompt", async (GetActiveEmailPrompt useCase, ClaimsPrincipal user, CancellationToken ct) =>
{
    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
    if (string.IsNullOrEmpty(userId))
        return Results.Unauthorized();

    var prompt = await useCase.Handle(userId, ct);
    return prompt == null ? Results.NotFound(new { error = "No active email prompt found" }) : Results.Ok(prompt);
}).RequireAuthorization();

// List all prompts
app.MapGet("/settings/email-prompts", async (ListEmailPrompts useCase, ClaimsPrincipal user, CancellationToken ct) =>
{
    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
    if (string.IsNullOrEmpty(userId))
        return Results.Unauthorized();

    var prompts = await useCase.Handle(userId, ct);
    return Results.Ok(prompts);
}).RequireAuthorization();

// Create new prompt
app.MapPost("/settings/email-prompts", async (CreateEmailPromptDto dto, CreateEmailPrompt useCase, ClaimsPrincipal user, CancellationToken ct) =>
{
    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
    if (string.IsNullOrEmpty(userId))
        return Results.Unauthorized();

    if (string.IsNullOrWhiteSpace(dto.Instructions))
        return Results.BadRequest(new { error = "Instructions are required" });

    try
    {
        var created = await useCase.Handle(userId, dto, ct);
        return Results.Created($"/settings/email-prompts/{created.Id}", created);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
}).RequireAuthorization();

// Update existing prompt
app.MapPut("/settings/email-prompts/{id:guid}", async (Guid id, UpdateEmailPromptDto dto, UpdateEmailPrompt useCase, ClaimsPrincipal user, CancellationToken ct) =>
{
    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
    if (string.IsNullOrEmpty(userId))
        return Results.Unauthorized();

    if (string.IsNullOrWhiteSpace(dto.Instructions))
        return Results.BadRequest(new { error = "Instructions are required" });

    try
    {
        var updated = await useCase.Handle(id, userId, dto, ct);
        return updated == null ? Results.NotFound() : Results.Ok(updated);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
}).RequireAuthorization();

// Activate specific prompt (deactivates all others)
app.MapPost("/settings/email-prompts/{id:guid}/activate", async (Guid id, ActivateEmailPrompt useCase, ClaimsPrincipal user, CancellationToken ct) =>
{
    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
    if (string.IsNullOrEmpty(userId))
        return Results.Unauthorized();

    var activated = await useCase.Handle(id, userId, ct);
    return activated == null ? Results.NotFound() : Results.Ok(activated);
}).RequireAuthorization();

// Delete prompt
app.MapDelete("/settings/email-prompts/{id:guid}", async (Guid id, DeleteEmailPrompt useCase, ClaimsPrincipal user, CancellationToken ct) =>
{
    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
    if (string.IsNullOrEmpty(userId))
        return Results.Unauthorized();

    try
    {
        var deleted = await useCase.Handle(id, userId, ct);
        return deleted ? Results.NoContent() : Results.NotFound();
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
}).RequireAuthorization();

// Legacy endpoint - kept for backwards compatibility
app.MapPut("/settings/email-prompt", async (
    UpdateEmailPromptDto dto, 
    UpdateEmailPrompt updateUseCase,
    GetActiveEmailPrompt getActiveUseCase, 
    ClaimsPrincipal user, 
    CancellationToken ct) =>
{
    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
    if (string.IsNullOrEmpty(userId))
        return Results.Unauthorized();

    if (string.IsNullOrWhiteSpace(dto.Instructions))
        return Results.BadRequest(new { error = "Instructions are required" });

    var activePrompt = await getActiveUseCase.Handle(userId, ct);
    if (activePrompt == null)
        return Results.NotFound(new { error = "No active email prompt found" });

    try
    {
        var updated = await updateUseCase.Handle(activePrompt.Id, userId, dto, ct);
        return updated == null ? Results.NotFound() : Results.Ok(updated);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
}).RequireAuthorization();

// ============ COMPANY INFO ENDPOINTS ============
var companyInfo = app.MapGroup("/settings/company-info").WithTags("Company Info");

// Get company info (read-only)
companyInfo.MapGet("/", async (GetCompanyInfo useCase, CancellationToken ct) =>
{
    try
    {
        var info = await useCase.Handle(ct);
        return Results.Ok(info);
    }
    catch (Exception ex)
    {
        return Results.Problem(
            detail: ex.Message,
            statusCode: 500,
            title: "Failed to load company info"
        );
    }
}).RequireAuthorization();


app.Run();
