using Esatto.Outreach.Application.DTOs.Auth;
using Esatto.Outreach.Application.UseCases.Auth;

namespace Esatto.Outreach.Api.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        var auth = app.MapGroup("/auth").WithTags("Authentication");

        auth.MapPost("/register", async (
            RegisterRequestDto dto,
            Register useCase,
            CancellationToken ct) =>
        {
            var (success, data, error) = await useCase.Handle(dto, ct);
            return success
                ? Results.Ok(data)
                : Results.BadRequest(new { error });
        });

        auth.MapPost("/login", async (
            LoginRequestDto dto,
            Login useCase,
            CancellationToken ct) =>
        {
            var (success, data, error) = await useCase.Handle(dto, ct);
            return success
                ? Results.Ok(data)
                : Results.BadRequest(new { error });
        });

        auth.MapPost("/refresh", async (
            RefreshTokenRequestDto dto,
            RefreshAccessToken useCase,
            CancellationToken ct) =>
        {
            var (success, data, error) = await useCase.Handle(dto, ct);
            return success
                ? Results.Ok(data)
                : Results.Unauthorized();
        });
    }
}
