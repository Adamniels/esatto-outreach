using Esatto.Outreach.Api.Requests.Auth;
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
            RegisterRequest req,
            RegisterCommandHandler handler,
            CancellationToken ct) =>
        {
            try
            {
                var data = await handler.Handle(new RegisterCommand(req.Email, req.Password, req.FullName, req.CompanyName), ct);
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
            LoginRequest req,
            LoginCommandHandler handler,
            CancellationToken ct) =>
        {
            try
            {
                var data = await handler.Handle(new LoginCommand(req.Email, req.Password), ct);
                return Results.Ok(data);
            }
            catch (AuthenticationFailedException)
            {
                return Results.Unauthorized();
            }
        });

        auth.MapPost("/refresh", async (
            RefreshTokenRequest req,
            RefreshAccessTokenCommandHandler handler,
            CancellationToken ct) =>
        {
            try
            {
                var data = await handler.Handle(new RefreshAccessTokenCommand(req.RefreshToken), ct);
                return Results.Ok(data);
            }
            catch (AuthenticationFailedException)
            {
                return Results.Unauthorized();
            }
        });
    }
}
