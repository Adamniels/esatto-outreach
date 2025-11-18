using Esatto.Outreach.Application.Abstractions;
using Esatto.Outreach.Application.DTOs;
using Esatto.Outreach.Application.UseCases.Prospects;
using Esatto.Outreach.Application.UseCases.EmailPrompts;
using Esatto.Outreach.Application.UseCases.EmailGeneration;
using Esatto.Outreach.Application.UseCases.EmailDelivery;
using Esatto.Outreach.Application.UseCases.SoftDataCollection;
using Esatto.Outreach.Application.UseCases.Chat;
using Esatto.Outreach.Infrastructure;
using Microsoft.OpenApi.Models;
using DotNetEnv;

// Läs .env filen från root-mappen
Env.Load("../.env");

var builder = WebApplication.CreateBuilder(args);

// Infrastructure (DbContext + repo via vår helper)
builder.Services.AddInfrastructure(builder.Configuration);

// Use-cases (enkelt att börja så här)
// en handling som systemet kan utföra
builder.Services.AddScoped<CreateProspect>();
builder.Services.AddScoped<UpdateProspect>();
builder.Services.AddScoped<GetProspectById>();
builder.Services.AddScoped<ListProspects>();
builder.Services.AddScoped<GenerateMailOpenAIResponeAPI>();
builder.Services.AddScoped<SendEmailViaN8n>();
builder.Services.AddScoped<ChatWithProspect>();
builder.Services.AddScoped<GenerateSoftCompanyData>();
builder.Services.AddScoped<ListEmailPrompts>();
builder.Services.AddScoped<GetActiveEmailPrompt>();
builder.Services.AddScoped<CreateEmailPrompt>();
builder.Services.AddScoped<UpdateEmailPrompt>();
builder.Services.AddScoped<ActivateEmailPrompt>();
builder.Services.AddScoped<DeleteEmailPrompt>();

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

app.UseCors("ui");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/healthz", () => Results.Ok(new { status = "ok" }));

// --- Prospects endpoints (via use-cases) ---

// Här injiceras usecaset från ovan
app.MapGet("/prospects", async (ListProspects useCase, CancellationToken ct) =>
{
    var list = await useCase.Handle(ct);
    return Results.Ok(list);
});

app.MapGet("/prospects/{id:guid}", async (Guid id, GetProspectById useCase, CancellationToken ct) =>
{
    var dto = await useCase.Handle(id, ct);
    return dto is null ? Results.NotFound() : Results.Ok(dto);
});

app.MapPost("/prospects", async (ProspectCreateDto dto, CreateProspect useCase, CancellationToken ct) =>
{
    if (string.IsNullOrWhiteSpace(dto.CompanyName))
        return Results.BadRequest(new { error = "CompanyName is required" });

    var created = await useCase.Handle(dto, ct);
    return Results.Created($"/prospects/{created.Id}", created);
});

app.MapPut("/prospects/{id:guid}", async (Guid id, ProspectUpdateDto dto, UpdateProspect useCase, CancellationToken ct) =>
{
    var updated = await useCase.Handle(id, dto, ct);
    return updated is null ? Results.NotFound() : Results.Ok(updated);
});

app.MapDelete("/prospects/{id:guid}", async (Guid id, IProspectRepository repo, CancellationToken ct) =>
{
    await repo.DeleteAsync(id, ct);
    return Results.NoContent();
});


app.MapPost("/prospects/{id:guid}/email/draft", async (
   Guid id,
   GenerateMailOpenAIResponeAPI useCase,
   CancellationToken ct) =>
{
    try
    {
        var draft = await useCase.Handle(id, ct);
        return Results.Ok(draft);
    }
    catch (InvalidOperationException ex)
    {
        // e.g. prospect not found
        return Results.NotFound(new { error = ex.Message });
    }
});

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

app.MapPost("/prospects/{id:guid}/chat/reset", async (Guid id, IProspectRepository repo, CancellationToken ct) =>
{
    var prospect = await repo.GetByIdAsync(id, ct);
    if (prospect is null) return Results.NotFound();
    prospect.SetLastOpenAIResponseId(null);
    await repo.UpdateAsync(prospect, ct);
    return Results.NoContent();
});

app.MapPost("/prospects/{id:guid}/soft-data/generate", async (
    Guid id,
    GenerateSoftCompanyData useCase,
    CancellationToken ct) =>
{
    try
    {
        var softData = await useCase.Handle(id, ct);
        return Results.Ok(softData);
    }
    catch (KeyNotFoundException ex)
    {
        return Results.NotFound(new { error = ex.Message });
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


// --- Email Prompt Settings endpoints ---

// Get active prompt
app.MapGet("/settings/email-prompt", async (GetActiveEmailPrompt useCase, CancellationToken ct) =>
{
    var prompt = await useCase.Handle(ct);
    return prompt == null ? Results.NotFound(new { error = "No active email prompt found" }) : Results.Ok(prompt);
});

// List all prompts
app.MapGet("/settings/email-prompts", async (ListEmailPrompts useCase, CancellationToken ct) =>
{
    var prompts = await useCase.Handle(ct);
    return Results.Ok(prompts);
});

// Create new prompt
app.MapPost("/settings/email-prompts", async (CreateEmailPromptDto dto, CreateEmailPrompt useCase, CancellationToken ct) =>
{
    if (string.IsNullOrWhiteSpace(dto.Instructions))
        return Results.BadRequest(new { error = "Instructions are required" });

    try
    {
        var created = await useCase.Handle(dto, ct);
        return Results.Created($"/settings/email-prompts/{created.Id}", created);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

// Update existing prompt
app.MapPut("/settings/email-prompts/{id:guid}", async (Guid id, UpdateEmailPromptDto dto, UpdateEmailPrompt useCase, CancellationToken ct) =>
{
    if (string.IsNullOrWhiteSpace(dto.Instructions))
        return Results.BadRequest(new { error = "Instructions are required" });

    try
    {
        var updated = await useCase.Handle(id, dto, ct);
        return updated == null ? Results.NotFound() : Results.Ok(updated);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

// Activate specific prompt (deactivates all others)
app.MapPost("/settings/email-prompts/{id:guid}/activate", async (Guid id, ActivateEmailPrompt useCase, CancellationToken ct) =>
{
    var activated = await useCase.Handle(id, ct);
    return activated == null ? Results.NotFound() : Results.Ok(activated);
});

// Delete prompt
app.MapDelete("/settings/email-prompts/{id:guid}", async (Guid id, DeleteEmailPrompt useCase, CancellationToken ct) =>
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
});

// Legacy endpoint - kept for backwards compatibility
app.MapPut("/settings/email-prompt", async (UpdateEmailPromptDto dto, UpdateEmailPrompt useCase, IGenerateEmailPromptRepository repo, CancellationToken ct) =>
{
    if (string.IsNullOrWhiteSpace(dto.Instructions))
        return Results.BadRequest(new { error = "Instructions are required" });

    var activePrompt = await repo.GetActiveAsync(ct);
    if (activePrompt == null)
        return Results.NotFound(new { error = "No active email prompt found" });

    try
    {
        var updated = await useCase.Handle(activePrompt.Id, dto, ct);
        return updated == null ? Results.NotFound() : Results.Ok(updated);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});


app.Run();
