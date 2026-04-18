using Esatto.Outreach.Application.Features.Auth.LoginUser;
using Esatto.Outreach.Application.Features.Auth.RefreshToken;
using Esatto.Outreach.Application.Features.Auth.RegisterUser;
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
            RegisterRequest dto,
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
            LoginRequest dto,
            LoginCommandHandler useCase,
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

        auth.MapPost("/refresh", async (
            RefreshTokenRequest dto,
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
