using System.Security.Claims;
using Esatto.Outreach.Application.DTOs;
using Esatto.Outreach.Application.UseCases.EmailPrompts;

namespace Esatto.Outreach.Api.Endpoints;

public static class EmailPromptEndpoints
{
    public static void MapEmailPromptEndpoints(this WebApplication app)
    {
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
    }
}
