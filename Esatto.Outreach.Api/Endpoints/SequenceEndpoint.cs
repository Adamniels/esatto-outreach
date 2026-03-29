using System.Security.Claims;

using Esatto.Outreach.Application.UseCases.Sequence;

namespace Esatto.Outreach.Api.Endpoints;

public static class SequenceEndpoints
{
    public static void MapSequenceEndpoints(this WebApplication app)
    {
        app.MapGet("/sequences", async (ListSequences useCase, ClaimsPrincipal user, CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();
            var list = await useCase.Handle(userId, ct);
            return Results.Ok(list);
        })
        .RequireAuthorization();
    }
}
