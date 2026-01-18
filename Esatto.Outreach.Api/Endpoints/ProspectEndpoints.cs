using System.Security.Claims;
using Esatto.Outreach.Application.DTOs;
using Esatto.Outreach.Application.UseCases.Prospects;
using Esatto.Outreach.Application.UseCases.EmailGeneration;
using Esatto.Outreach.Application.UseCases.EmailDelivery;
using Esatto.Outreach.Application.UseCases.SoftDataCollection;
using Esatto.Outreach.Application.UseCases.Chat;

namespace Esatto.Outreach.Api.Endpoints;

public static class ProspectEndpoints
{
    public static void MapProspectEndpoints(this WebApplication app)
    {
        // ============ PROSPECT CRUD ENDPOINTS ============

        // Use case is injected from the service container
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

        // ============ CONTACT PERSON ENDPOINTS ============

        app.MapPost("/prospects/{id:guid}/contacts", async (
            Guid id,
            CreateContactPersonDto dto,
            AddContactPerson useCase,
            ClaimsPrincipal user,
            CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

            if (string.IsNullOrWhiteSpace(dto.Name))
                return Results.BadRequest(new { error = "Name is required" });

            try
            {
                var created = await useCase.Handle(id, dto, ct);
                return created is null ? Results.NotFound() : Results.Ok(created);
            }
            catch (InvalidOperationException ex)
            {
                return Results.Conflict(new { error = ex.Message });
            }
        })
        .RequireAuthorization();

        app.MapPut("/prospects/{prospectId:guid}/contacts/{contactId:guid}", async (
            Guid prospectId, 
            Guid contactId, 
            UpdateContactPersonDto dto, 
            UpdateContactPerson useCase, 
            ClaimsPrincipal user, 
            CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

            var updated = await useCase.Handle(prospectId, contactId, dto, ct);
            return updated is null ? Results.NotFound() : Results.Ok(updated);
        })
        .RequireAuthorization();

        app.MapDelete("/prospects/{prospectId:guid}/contacts/{contactId:guid}", async (
            Guid prospectId, 
            Guid contactId, 
            DeleteContactPerson useCase, 
            ClaimsPrincipal user, 
            CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

            var success = await useCase.Handle(prospectId, contactId, ct);
            return success ? Results.Ok() : Results.NotFound();
        })
        .RequireAuthorization();

        app.MapPost("/prospects/{prospectId:guid}/contacts/{contactId:guid}/enrich", async (
            Guid prospectId,
            Guid contactId,
            EnrichContactPerson useCase,
            ClaimsPrincipal user,
            CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

            var enriched = await useCase.Handle(contactId, userId, ct);
            return enriched is null ? Results.NotFound() : Results.Ok(enriched);
        })
        .RequireAuthorization();

        // ============ EMAIL ENDPOINTS ============

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
                return Results.NotFound(new { error = ex.Message });
            }
            catch (ArgumentException ex)
            {
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

        // ============ CHAT ENDPOINTS ============

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

        // ============ SOFT DATA / ENRICHMENT ============

        app.MapPost("/prospects/{id:guid}/soft-data/generate", async (
            Guid id,
            GenerateEntityIntelligence useCase,
            ILogger<Program> logger,
            CancellationToken ct) =>
        {
            try
            {
                var softData = await useCase.Handle(id, ct);
                return Results.Ok(softData);
            }
            catch (KeyNotFoundException ex)
            {
                logger.LogWarning(ex, "Prospect {Id} not found during enrichment", id);
                return Results.NotFound(new { error = ex.Message });
            }
            catch (ArgumentException ex)
            {
                logger.LogWarning(ex, "Invalid argument during enrichment for {Id}", id);
                return Results.BadRequest(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                logger.LogWarning(ex, "Invalid operation during enrichment for {Id}", id);
                return Results.BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to enrich prospect {Id}", id);
                return Results.Json(new { error = ex.Message }, statusCode: 500);
            }
        });
    }
}
