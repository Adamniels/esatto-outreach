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

// CORS – tillåt n8n/valfritt UI
builder.Services.AddCors(opt =>
{
    opt.AddPolicy("ui", p => p
        .AllowAnyHeader().AllowAnyMethod()
        .WithOrigins("http://localhost:5678", "http://localhost:3000"));
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

app.Run();
