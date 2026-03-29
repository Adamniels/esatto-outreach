using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Esatto.Outreach.Application.DTOs.Auth;
using Esatto.Outreach.Application.UseCases.Auth;
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
            try
            {
                var data = await useCase.Handle(dto, ct);
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

            try
            {
                var frontendBaseUrl = configuration["Frontend:BaseUrl"];
                var data = await useCase.Handle(userId, dto.Email, frontendBaseUrl, ct);
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
    }
}
