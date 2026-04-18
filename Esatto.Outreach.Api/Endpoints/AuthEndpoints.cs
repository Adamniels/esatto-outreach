using Esatto.Outreach.Application.Features.Auth;
using Esatto.Outreach.Application.Features.Auth;
using Esatto.Outreach.Domain.Exceptions;

namespace Esatto.Outreach.Api.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        var auth = app.MapGroup("/auth")
                      .WithTags("Authentication")
                      .RequireRateLimiting("AuthPolicy");

        auth.MapPost("/register", async (
            RegisterRequestDto dto,
            RegisterCommandHandler useCase,
            CancellationToken ct) =>
        {
            try
            {
                var data = await useCase.Handle(dto, ct);
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
        });

        auth.MapPost("/login", async (
            LoginRequestDto dto,
            LoginCommandHandler useCase,
            CancellationToken ct) =>
        {
            try
            {
                var data = await useCase.Handle(dto, ct);
                return Results.Ok(data);
            }
            catch (AuthenticationFailedException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        auth.MapPost("/refresh", async (
            RefreshTokenRequestDto dto,
            RefreshAccessTokenCommandHandler useCase,
            CancellationToken ct) =>
        {
            try
            {
                var data = await useCase.Handle(dto, ct);
                return Results.Ok(data);
            }
            catch (AuthenticationFailedException)
            {
                return Results.Unauthorized();
            }
        });
    }
}
