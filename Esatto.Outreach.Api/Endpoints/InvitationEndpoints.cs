using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Esatto.Outreach.Application.DTOs;
using Esatto.Outreach.Application.DTOs.Prospects;
using Esatto.Outreach.Application.DTOs.Auth;
using Esatto.Outreach.Application.DTOs.Intelligence;
using Esatto.Outreach.Application.DTOs.Outreach;
using Esatto.Outreach.Application.DTOs.Webhooks;
using Esatto.Outreach.Application.DTOs.Workflows;
using Esatto.Outreach.Application.UseCases.Auth;

namespace Esatto.Outreach.Api.Endpoints;

public static class InvitationEndpoints
{
    public static void MapInvitationEndpoints(this WebApplication app)
    {
        var invitations = app.MapGroup("/invitations")
                             .WithTags("Invitations")
                             .RequireRateLimiting("AuthPolicy");

        invitations.MapGet("/validate", async (
            [FromQuery] string? token,
            ValidateInvitation useCase,
            CancellationToken ct) =>
        {
            if (string.IsNullOrEmpty(token))
                return Results.BadRequest(new { error = "Invalid or expired invitation" });

            var result = await useCase.Handle(token, ct);
            if (result == null)
                return Results.BadRequest(new { error = "Invalid or expired invitation" });

            return Results.Ok(result);
        });

        invitations.MapPost("/accept", async (
            AcceptInvitationDto dto,
            AcceptInvitation useCase,
            CancellationToken ct) =>
        {
            var (success, data, error) = await useCase.Handle(dto, ct);
            if (success)
                return Results.Ok(data);
            
            // Return generic 400 Bad Request to prevent enumeration, unless we want to leak specifically
            return Results.BadRequest(new { error = "Invalid or expired invitation" });
        });

        app.MapPost("/company/invitations", async (
            CreateInvitationDto dto,
            CreateInvitation useCase,
            ClaimsPrincipal user,
            IConfiguration configuration,
            CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Results.Unauthorized();

            var frontendBaseUrl = configuration["Frontend:BaseUrl"];
            var (success, data, error) = await useCase.Handle(userId, dto.Email, frontendBaseUrl, ct);
            if (!success)
                return Results.BadRequest(new { error });
            return Results.Ok(data);
        })
        .RequireAuthorization();
    }
}
