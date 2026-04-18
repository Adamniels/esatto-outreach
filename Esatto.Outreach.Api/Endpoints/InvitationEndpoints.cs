using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Esatto.Outreach.Application.Features.Auth.AcceptInvitation;
using Esatto.Outreach.Application.Features.Auth.InviteUser;
using Esatto.Outreach.Application.Features.Auth.ValidateInvitation;
using Esatto.Outreach.Domain.Exceptions;

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
            ValidateInvitationCommandHandler handler,
            CancellationToken ct) =>
        {
            if (string.IsNullOrEmpty(token))
                return Results.BadRequest(new { error = "Invalid or expired invitation" });

            var result = await handler.Handle(new ValidateInvitationCommand(token), ct);
            if (result == null)
                return Results.BadRequest(new { error = "Invalid or expired invitation" });

            return Results.Ok(result);
        });

        invitations.MapPost("/accept", async (
            AcceptInvitationCommand dto,
            AcceptInvitationCommandHandler handler,
            CancellationToken ct) =>
        {
            try
            {
                var data = await handler.Handle(dto, ct);
                return Results.Ok(data);
            }
            catch (AuthenticationFailedException)
            {
                return Results.BadRequest(new { error = "Invalid or expired invitation" });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        invitations.MapPost("/", async (
            InviteUserCommand dto,
            InviteUserCommandHandler handler,
            ClaimsPrincipal user,
            IConfiguration configuration,
            CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Results.Unauthorized();

            try
            {
                var frontendBaseUrl = configuration["Frontend:BaseUrl"];
                var data = await handler.Handle(dto with { FrontendBaseUrl = frontendBaseUrl }, userId, ct);
                return Results.Ok(data);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        })
        .RequireAuthorization();

        // Backward-compatible legacy route.
        app.MapPost("/company/invitations", async (
            InviteUserCommand dto,
            InviteUserCommandHandler handler,
            ClaimsPrincipal user,
            IConfiguration configuration,
            CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Results.Unauthorized();

            try
            {
                var frontendBaseUrl = configuration["Frontend:BaseUrl"];
                var data = await handler.Handle(dto with { FrontendBaseUrl = frontendBaseUrl }, userId, ct);
                return Results.Ok(data);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        }).RequireAuthorization();
    }
}
