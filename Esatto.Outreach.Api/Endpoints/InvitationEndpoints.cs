using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Esatto.Outreach.Api.Requests.Auth;
using Esatto.Outreach.Application.Features.Auth.AcceptInvitation;
using Esatto.Outreach.Domain.Exceptions;
using Esatto.Outreach.Application.Features.Auth.InviteUser;
using Esatto.Outreach.Application.Features.Auth.ValidateInvitation;

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
            AcceptInvitationRequest req,
            AcceptInvitationCommandHandler handler,
            CancellationToken ct) =>
        {
            try
            {
                var data = await handler.Handle(new AcceptInvitationCommand(req.Token, req.Email, req.Password, req.FullName), ct);
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

        static async Task<IResult> InviteUser(
            InviteUserRequest req,
            InviteUserCommandHandler handler,
            ClaimsPrincipal user,
            CancellationToken ct)
        {
            var userId = user.GetRequiredUserId();
            if (string.IsNullOrEmpty(userId))
                return Results.Unauthorized();

            var data = await handler.Handle(new InviteUserCommand(req.Email), userId, ct);
            return Results.Ok(data);
        }

        invitations.MapPost("/", InviteUser).RequireAuthorization();

        // Backward-compatible legacy route.
        app.MapPost("/company/invitations", InviteUser).RequireAuthorization();
    }
}
