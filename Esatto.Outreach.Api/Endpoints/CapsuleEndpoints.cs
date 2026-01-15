using System.Security.Claims;
using Esatto.Outreach.Application.DTOs;
using Esatto.Outreach.Application.UseCases.CapsuleDataSource;

namespace Esatto.Outreach.Api.Endpoints;

public static class CapsuleEndpoints
{
    public static void MapCapsuleEndpoints(this WebApplication app)
    {
        // Webhook endpoint for Capsule CRM (public, no auth)
        app.MapPost("/webhooks/capsule", async (
            HttpContext httpContext,
            HandleCapsuleWebhook useCase,
            ILogger<Program> logger,
            CancellationToken ct) =>
        {
            try
            {
                // Read raw body for debugging
                httpContext.Request.EnableBuffering();
                using var reader = new StreamReader(httpContext.Request.Body, leaveOpen: true);
                var rawBody = await reader.ReadToEndAsync(ct);
                httpContext.Request.Body.Position = 0;
                logger.LogInformation("=== CAPSULE WEBHOOK RECEIVED ===");
                logger.LogInformation("Raw Body: {RawBody}", rawBody);
                logger.LogInformation("Content-Type: {ContentType}", httpContext.Request.ContentType);

                // Attempt to deserialize
                CapsuleWebhookEventDto? payload;
                try
                {
                    payload = await httpContext.Request.ReadFromJsonAsync<CapsuleWebhookEventDto>(ct);
                    logger.LogInformation("Successfully deserialized payload. Event: {Event}, Payload count: {Count}",
                        payload?.Type, payload?.Payload?.Count);
                }
                catch (Exception deserEx)
                {
                    logger.LogError(deserEx, "Failed to deserialize webhook payload");
                    return Results.BadRequest(new { error = "Invalid JSON format", details = deserEx.Message });
                }
                if (payload == null)
                {
                    logger.LogWarning("Payload is null after deserialization");
                    return Results.BadRequest(new { error = "Payload is required" });
                }
                var result = await useCase.Handle(payload, ct);
                logger.LogInformation("Webhook processed. Success: {Success}, Message: {Message}",
                    result.Success, result.Message);
                return result.Success
                    ? Results.Ok(result)
                    : Results.BadRequest(result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unhandled exception in Capsule webhook endpoint");
                return Results.Problem(
                    detail: ex.Message,
                    statusCode: 500,
                    title: "Failed to process Capsule webhook");
            }
        });

        // List pending prospects (from Capsule, not yet claimed)
        app.MapGet("/prospects/pending", async (
            ListPendingProspects useCase,
            CancellationToken ct) =>
        {
            try
            {
                var pending = await useCase.Handle(ct);
                return Results.Ok(pending);
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    detail: ex.Message,
                    statusCode: 500);
            }
        })
        .RequireAuthorization();

        // Claim a pending prospect
        app.MapPost("/prospects/{id:guid}/claim", async (
            Guid id,
            ClaimPendingProspect useCase,
            ClaimsPrincipal user,
            CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Results.Unauthorized();

            try
            {
                var claimed = await useCase.Handle(id, userId, ct);
                return claimed == null ? Results.NotFound() : Results.Ok(claimed);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        })
        .RequireAuthorization();

        // Reject a pending prospect (delete it)
        app.MapPost("/prospects/{id:guid}/pending/reject", async (
            Guid id,
            RejectPendingProspect useCase,
            CancellationToken ct) =>
        {
            try
            {
                var deleted = await useCase.Handle(id, ct);
                return deleted ? Results.NoContent() : Results.NotFound();
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        })
        .RequireAuthorization();
    }
}
