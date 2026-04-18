using System.Security.Claims;
using Esatto.Outreach.Application.Features.OutreachPrompts.ActivateOutreachPrompt;
using Esatto.Outreach.Application.Features.OutreachPrompts.CreateOutreachPrompt;
using Esatto.Outreach.Application.Features.OutreachPrompts.DeleteOutreachPrompt;
using Esatto.Outreach.Application.Features.OutreachPrompts.GetActiveOutreachPrompt;
using Esatto.Outreach.Application.Features.OutreachPrompts.ListOutreachPrompts;
using Esatto.Outreach.Application.Features.OutreachPrompts.UpdateOutreachPrompt;
using Esatto.Outreach.Domain.Enums;

namespace Esatto.Outreach.Api.Endpoints;

public static class OutreachPromptEndpoints
{
    public static void MapOutreachPromptEndpoints(this WebApplication app)
    {
        // Get active prompt by type
        app.MapGet("/settings/outreach-prompts/active/{type}", async (PromptType type, GetActiveOutreachPromptQueryHandler handler, ClaimsPrincipal user, CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Results.Unauthorized();

            var prompt = await handler.Handle(new GetActiveOutreachPromptQuery(type), userId, ct);
            return prompt == null ? Results.NotFound(new { error = $"No active {type} prompt found" }) : Results.Ok(prompt);
        }).RequireAuthorization();

        // List all prompts
        app.MapGet("/settings/outreach-prompts", async (ListOutreachPromptsQueryHandler handler, ClaimsPrincipal user, CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Results.Unauthorized();

            var prompts = await handler.Handle(new ListOutreachPromptsQuery(), userId, ct);
            return Results.Ok(prompts);
        }).RequireAuthorization();

        // Create new prompt
        app.MapPost("/settings/outreach-prompts", async (CreateOutreachPromptCommand dto, CreateOutreachPromptCommandHandler handler, ClaimsPrincipal user, CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Results.Unauthorized();

            if (string.IsNullOrWhiteSpace(dto.Instructions))
                return Results.BadRequest(new { error = "Instructions are required" });

            try
            {
                var created = await handler.Handle(dto, userId, ct);
                return Results.Created($"/settings/outreach-prompts/{created.Id}", created);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        }).RequireAuthorization();

        // Update existing prompt
        app.MapPut("/settings/outreach-prompts/{id:guid}", async (Guid id, UpdateOutreachPromptCommand dto, UpdateOutreachPromptCommandHandler handler, ClaimsPrincipal user, CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Results.Unauthorized();

            if (string.IsNullOrWhiteSpace(dto.Instructions))
                return Results.BadRequest(new { error = "Instructions are required" });

            try
            {
                var updated = await handler.Handle(dto with { Id = id }, userId, ct);
                return updated == null ? Results.NotFound() : Results.Ok(updated);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        }).RequireAuthorization();

        // Activate specific prompt (deactivates all others)
        app.MapPost("/settings/outreach-prompts/{id:guid}/activate", async (Guid id, ActivateOutreachPromptCommandHandler handler, ClaimsPrincipal user, CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Results.Unauthorized();

            var activated = await handler.Handle(new ActivateOutreachPromptCommand(id), userId, ct);
            return activated == null ? Results.NotFound() : Results.Ok(activated);
        }).RequireAuthorization();

        // Delete prompt
        app.MapDelete("/settings/outreach-prompts/{id:guid}", async (Guid id, DeleteOutreachPromptCommandHandler handler, ClaimsPrincipal user, CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Results.Unauthorized();

            try
            {
                var deleted = await handler.Handle(new DeleteOutreachPromptCommand(id), userId, ct);
                return deleted ? Results.NoContent() : Results.NotFound();
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        }).RequireAuthorization();
    }
}
