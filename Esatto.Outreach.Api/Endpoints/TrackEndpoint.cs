using System.Security.Claims;

using Esatto.Outreach.Application.UseCases.Track;

namespace Esatto.Outreach.Api.Endpoints;

public static class TrackEndpoints
{
    public static void MapTrackEndpoints(this WebApplication app)
    {
        app.MapGet("/tracks", async (ListTracks useCase, ClaimsPrincipal user, CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();
            var list = await useCase.Handle(userId, ct);
            return Results.Ok(list);
        })
        .RequireAuthorization();
    }
}