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
            RegisterCommand dto,
            RegisterCommandHandler handler,
            CancellationToken ct) =>
        {
            try
            {
                var data = await handler.Handle(dto, ct);
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
            LoginCommand dto,
            LoginCommandHandler handler,
            CancellationToken ct) =>
        {
            try
            {
                var data = await handler.Handle(dto, ct);
                return Results.Ok(data);
            }
            catch (AuthenticationFailedException)
            {
                return Results.Unauthorized();
            }
        });

        auth.MapPost("/refresh", async (
            RefreshAccessTokenCommand dto,
            RefreshAccessTokenCommandHandler handler,
            CancellationToken ct) =>
        {
            try
            {
                var data = await handler.Handle(dto, ct);
                return Results.Ok(data);
            }
            catch (AuthenticationFailedException)
            {
                return Results.Unauthorized();
            }
        });
    }
}
