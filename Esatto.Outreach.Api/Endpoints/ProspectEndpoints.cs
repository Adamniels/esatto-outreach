using System.Security.Claims;
using Esatto.Outreach.Application.Features.Prospects.AddContactPerson;
using Esatto.Outreach.Application.Features.Prospects.ClearActiveContact;
using Esatto.Outreach.Application.Features.Prospects.CreateProspect;
using Esatto.Outreach.Application.Features.Prospects.DeleteContactPerson;
using Esatto.Outreach.Application.Features.Prospects.DeleteProspect;
using Esatto.Outreach.Application.Features.Prospects.GetActiveContact;
using Esatto.Outreach.Application.Features.Prospects.GetProspectById;
using Esatto.Outreach.Application.Features.Prospects.ListProspects;
using Esatto.Outreach.Application.Features.Prospects.SetActiveContact;
using Esatto.Outreach.Application.Features.Prospects.UpdateContactPerson;
using Esatto.Outreach.Application.Features.Prospects.UpdateProspect;
using Esatto.Outreach.Application.Features.Intelligence.ChatWithProspect;
using Esatto.Outreach.Application.Features.Intelligence.EnrichContactPerson;
using Esatto.Outreach.Application.Features.Intelligence.GenerateEntityIntelligence;
using Esatto.Outreach.Application.Features.Intelligence.ResetProspectChat;
using Esatto.Outreach.Application.Features.OutreachGeneration.GenerateEmailDraft;
using Esatto.Outreach.Application.Features.OutreachGeneration.GenerateLinkedInDraft;

namespace Esatto.Outreach.Api.Endpoints;

public static class ProspectEndpoints
{
    public static void MapProspectEndpoints(this WebApplication app)
    {
        // ============ PROSPECT CRUD ENDPOINTS ============

        // Use case is injected from the service container
        app.MapGet("/prospects", async (ListProspectsQueryHandler handler, ClaimsPrincipal user, CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();
            var list = await handler.Handle(new ListProspectsQuery(), userId, ct);
            return Results.Ok(list);
        })
        .RequireAuthorization();

        app.MapGet("/prospects/{id:guid}", async (Guid id, GetProspectByIdQueryHandler handler, ClaimsPrincipal user, CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

            var dto = await handler.Handle(new GetProspectByIdQuery(id), userId, ct);
            return dto is null ? Results.NotFound() : Results.Ok(dto);
        })
        .RequireAuthorization();

        app.MapPost("/prospects", async (CreateProspectCommand command, CreateProspectCommandHandler handler, ClaimsPrincipal user, CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

            try
            {
                var created = await handler.Handle(command, userId, ct);
                return Results.Created($"/prospects/{created.Id}", created);
            }
            catch (ArgumentException ex) { return Results.BadRequest(new { error = ex.Message }); }
        })
        .RequireAuthorization();

        app.MapPut("/prospects/{id:guid}", async (Guid id, UpdateProspectCommand command, UpdateProspectCommandHandler handler, ClaimsPrincipal user, CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();
            try
            {
                var updated = await handler.Handle(command with { Id = id }, userId, ct);
                return updated is null ? Results.NotFound() : Results.Ok(updated);
            }
            catch (UnauthorizedAccessException) { return Results.StatusCode(403); }
            catch (ArgumentException ex) { return Results.BadRequest(new { error = ex.Message }); }
        })
        .RequireAuthorization();

        app.MapDelete("/prospects/{id:guid}", async (Guid id, DeleteProspectCommandHandler handler, ClaimsPrincipal user, CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();
            try
            {
                var deleted = await handler.Handle(new DeleteProspectCommand(id), userId, ct);
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
            AddContactPersonCommand command,
            AddContactPersonCommandHandler handler,
            ClaimsPrincipal user,
            CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

            try
            {
                var created = await handler.Handle(command with { ProspectId = id }, ct);
                return created is null ? Results.NotFound() : Results.Ok(created);
            }
            catch (InvalidOperationException ex) { return Results.Conflict(new { error = ex.Message }); }
            catch (ArgumentException ex) { return Results.BadRequest(new { error = ex.Message }); }
        })
        .RequireAuthorization();

        app.MapPut("/prospects/{prospectId:guid}/contacts/{contactId:guid}", async (
            Guid prospectId,
            Guid contactId,
            UpdateContactPersonCommand command,
            UpdateContactPersonCommandHandler handler,
            ClaimsPrincipal user,
            CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

            var updated = await handler.Handle(command with { ProspectId = prospectId, ContactId = contactId }, ct);
            return updated is null ? Results.NotFound() : Results.Ok(updated);
        })
        .RequireAuthorization();

        app.MapDelete("/prospects/{prospectId:guid}/contacts/{contactId:guid}", async (
            Guid prospectId,
            Guid contactId,
            DeleteContactPersonCommandHandler handler,
            ClaimsPrincipal user,
            CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

            var success = await handler.Handle(new DeleteContactPersonCommand(prospectId, contactId), ct);
            return success ? Results.Ok() : Results.NotFound();
        })
        .RequireAuthorization();

        app.MapPost("/prospects/{prospectId:guid}/contacts/{contactId:guid}/enrich", async (
            Guid prospectId,
            Guid contactId,
            EnrichContactPersonCommandHandler handler,
            ClaimsPrincipal user,
            CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

            var enriched = await handler.Handle(new EnrichContactPersonCommand(contactId), userId, ct);
            return enriched is null ? Results.NotFound() : Results.Ok(enriched);
        })
        .RequireAuthorization();

        // ============ ACTIVE CONTACT ENDPOINTS ============

        app.MapPost("/prospects/{prospectId:guid}/contacts/{contactId:guid}/activate", async (
            Guid prospectId,
            Guid contactId,
            SetActiveContactCommandHandler handler,
            ClaimsPrincipal user,
            CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

            try
            {
                await handler.Handle(new SetActiveContactCommand(prospectId, contactId), userId, ct);
                return Results.Ok(new { success = true });
            }
            catch (InvalidOperationException ex)
            {
                return Results.NotFound(new { error = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
            catch (UnauthorizedAccessException)
            {
                return Results.StatusCode(403);
            }
        })
        .RequireAuthorization();

        app.MapDelete("/prospects/{prospectId:guid}/contacts/active", async (
            Guid prospectId,
            ClearActiveContactCommandHandler handler,
            ClaimsPrincipal user,
            CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

            try
            {
                await handler.Handle(new ClearActiveContactCommand(prospectId), userId, ct);
                return Results.NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return Results.NotFound(new { error = ex.Message });
            }
            catch (UnauthorizedAccessException)
            {
                return Results.StatusCode(403);
            }
        })
        .RequireAuthorization();

        app.MapGet("/prospects/{prospectId:guid}/contacts/active", async (
            Guid prospectId,
            GetActiveContactQueryHandler handler,
            ClaimsPrincipal user,
            CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

            try
            {
                var contact = await handler.Handle(new GetActiveContactQuery(prospectId), userId, ct);
                return contact is null ? Results.NotFound() : Results.Ok(contact);
            }
            catch (UnauthorizedAccessException)
            {
                return Results.StatusCode(403);
            }
        })
        .RequireAuthorization();

        // ============ EMAIL ENDPOINTS ============

        app.MapPost("/prospects/{id:guid}/email/draft", async (
           Guid id,
           GenerateMailCommandHandler handler,
           ClaimsPrincipal user,
           string? type,
           CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Results.Unauthorized();

            try
            {
                var prospect = await handler.Handle(new GenerateMailCommand(id, type), userId, ct);
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

        // ============ LINKEDIN ENDPOINTS ============
        // LinkedIn message generation is similar to email, but we can have a separate endpoint for clarity

        app.MapPost("/prospects/{id:guid}/linkedin/draft", async (
           Guid id,
           GenerateLinkedInMessageCommandHandler handler,
           ClaimsPrincipal user,
           string? type,
           CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Results.Unauthorized();

            try
            {
                var prospect = await handler.Handle(new GenerateLinkedInMessageCommand(id, type), userId, ct);
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

        // ============ CHAT ENDPOINTS ============

        app.MapPost("/prospects/{id:guid}/chat", async (Guid id, ChatWithProspectCommand dto, ChatWithProspectCommandHandler handler, ClaimsPrincipal user, CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

            try
            {
                var res = await handler.Handle(dto with { ProspectId = id }, userId, ct);
                return Results.Ok(res);
            }
            catch (InvalidOperationException ex)
            {
                return Results.NotFound(new { error = ex.Message });
            }
            catch (UnauthorizedAccessException)
            {
                return Results.StatusCode(403);
            }
        }).RequireAuthorization();

        app.MapPost("/prospects/{id:guid}/chat/reset", async (Guid id, ResetProspectChatCommandHandler handler, CancellationToken ct) =>
        {
            var success = await handler.Handle(new ResetProspectChatCommand(id), ct);
            return success ? Results.NoContent() : Results.NotFound();
        })
        .RequireAuthorization();

        // ============ SOFT DATA / ENRICHMENT ============

        app.MapPost("/prospects/{id:guid}/soft-data/generate", async (
            Guid id,
            GenerateEntityIntelligenceCommandHandler handler,
            ILogger<Program> logger,
            ClaimsPrincipal user,
            CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

            try
            {
                var softData = await handler.Handle(new GenerateEntityIntelligenceCommand(id), userId, ct);
                return Results.Ok(softData);
            }
            catch (UnauthorizedAccessException)
            {
                return Results.StatusCode(403);
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
        }).RequireAuthorization();
    }
}
