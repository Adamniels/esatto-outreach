using System.Security.Claims;
using Esatto.Outreach.Application.DTOs;
using Esatto.Outreach.Application.UseCases.Batch;

namespace Esatto.Outreach.Api.Endpoints;

public static class BatchEndpoints
{
    public static void MapBatchEndpoints(this WebApplication app)
    {
        app.MapPost("/prospects/batch/soft-data/generate", async (
            BatchSoftDataRequest request,
            GenerateEntityIntelligenceBatch useCase,
            ClaimsPrincipal user,
            CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Results.Unauthorized();

            try
            {
                var results = await useCase.Handle(request.ProspectIds, userId, ct);
                return Results.Ok(results);
            }
            catch (UnauthorizedAccessException)
            {
                return Results.StatusCode(403); // Forbidden
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    detail: ex.Message,
                    statusCode: 500);
            }
        })
        .RequireAuthorization();

        app.MapPost("/prospects/batch/email/generate", async (
            BatchEmailRequest request,
            GenerateEmailBatch useCase,
            ClaimsPrincipal user,
            CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Results.Unauthorized();

            try
            {
                var results = await useCase.Handle(
                    request.ProspectIds,
                    userId,
                    request.Type,
                    request.AutoGenerateSoftData,
                    ct);
                return Results.Ok(results);
            }
            catch (UnauthorizedAccessException)
            {
                return Results.StatusCode(403); // Forbidden
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    detail: ex.Message,
                    statusCode: 500);
            }
        })
        .RequireAuthorization();
    }
}
