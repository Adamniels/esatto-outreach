using System.Security.Claims;
using Esatto.Outreach.Application.DTOs;
using Esatto.Outreach.Application.DTOs.Prospects;
using Esatto.Outreach.Application.DTOs.Auth;
using Esatto.Outreach.Application.DTOs.Intelligence;
using Esatto.Outreach.Application.DTOs.Outreach;
using Esatto.Outreach.Application.DTOs.Webhooks;
using Esatto.Outreach.Application.DTOs.Workflows;
using Esatto.Outreach.Application.UseCases.OutreachPrompts;
using Esatto.Outreach.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace Esatto.Outreach.Api.Endpoints;

public static class OutreachPromptEndpoints
{
    public static void MapOutreachPromptEndpoints(this WebApplication app)
    {
        // Get active prompt by type
        app.MapGet("/settings/outreach-prompts/active/{type}", async (PromptType type, GetActiveOutreachPrompt useCase, ClaimsPrincipal user, CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Results.Unauthorized();

            var prompt = await useCase.Handle(userId, type, ct);
            return prompt == null ? Results.NotFound(new { error = $"No active {type} prompt found" }) : Results.Ok(prompt);
        }).RequireAuthorization();

        // List all prompts
        app.MapGet("/settings/outreach-prompts", async (ListOutreachPrompts useCase, ClaimsPrincipal user, CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Results.Unauthorized();

            var prompts = await useCase.Handle(userId, ct);
            return Results.Ok(prompts);
        }).RequireAuthorization();

        // Create new prompt
        app.MapPost("/settings/outreach-prompts", async (CreateOutreachPromptDto dto, CreateOutreachPrompt useCase, ClaimsPrincipal user, CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Results.Unauthorized();

            if (string.IsNullOrWhiteSpace(dto.Instructions))
                return Results.BadRequest(new { error = "Instructions are required" });

            try
            {
                var created = await useCase.Handle(userId, dto, ct);
                return Results.Created($"/settings/outreach-prompts/{created.Id}", created);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        }).RequireAuthorization();

        // Update existing prompt
        app.MapPut("/settings/outreach-prompts/{id:guid}", async (Guid id, UpdateOutreachPromptDto dto, UpdateOutreachPrompt useCase, ClaimsPrincipal user, CancellationToken ct) =>
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
        app.MapPost("/settings/outreach-prompts/{id:guid}/activate", async (Guid id, ActivateOutreachPrompt useCase, ClaimsPrincipal user, CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Results.Unauthorized();

            var activated = await useCase.Handle(id, userId, ct);
            return activated == null ? Results.NotFound() : Results.Ok(activated);
        }).RequireAuthorization();

        // Delete prompt
        app.MapDelete("/settings/outreach-prompts/{id:guid}", async (Guid id, DeleteOutreachPrompt useCase, ClaimsPrincipal user, CancellationToken ct) =>
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
