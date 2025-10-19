using Esatto.Outreach.Application.Abstractions;
using Esatto.Outreach.Application.DTOs;
using Esatto.Outreach.Application.UseCases;
using Esatto.Outreach.Infrastructure;
using Microsoft.OpenApi.Models;

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


app.Run();
