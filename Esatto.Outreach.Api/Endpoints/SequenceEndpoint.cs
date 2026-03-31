using System.Security.Claims;
using Esatto.Outreach.Application.UseCases.Sequences;
using Esatto.Outreach.Application.DTOs.Sequence;

namespace Esatto.Outreach.Api.Endpoints;

public static class SequenceEndpoints
{
    public static void MapSequenceEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/sequences")
                       .RequireAuthorization();

        // Sequences CRUD
        group.MapPost("/", async (CreateSequenceRequest req, CreateSequence useCase, ClaimsPrincipal user, CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();
            try { return Results.Ok(await useCase.Handle(req, userId, ct)); }
            catch (ArgumentException e) { return Results.BadRequest(new { error = e.Message }); }
        });

        group.MapGet("/", async (ListSequences useCase, ClaimsPrincipal user, CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();
            return Results.Ok(await useCase.Handle(userId, ct));
        });

        group.MapGet("/{id:guid}", async (Guid id, GetSequence useCase, ClaimsPrincipal user, CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();
            try { return Results.Ok(await useCase.Handle(id, userId, ct)); }
            catch (KeyNotFoundException e) { return Results.NotFound(new { error = e.Message }); }
        });

        group.MapPut("/{id:guid}", async (Guid id, UpdateSequenceRequest req, UpdateSequence useCase, ClaimsPrincipal user, CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();
            try { return Results.Ok(await useCase.Handle(id, req, userId, ct)); }
            catch (KeyNotFoundException e) { return Results.NotFound(new { error = e.Message }); }
            catch (ArgumentException e) { return Results.BadRequest(new { error = e.Message }); }
        });

        group.MapDelete("/{id:guid}", async (Guid id, DeleteSequence useCase, ClaimsPrincipal user, CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();
            try { await useCase.Handle(id, userId, ct); return Results.NoContent(); }
            catch (KeyNotFoundException e) { return Results.NotFound(new { error = e.Message }); }
            catch (InvalidOperationException e) { return Results.BadRequest(new { error = e.Message }); }
        });

        // Step Management
        group.MapPost("/{id:guid}/steps", async (Guid id, AddSequenceStepRequest req, AddSequenceStep useCase, ClaimsPrincipal user, CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();
            try { return Results.Ok(await useCase.Handle(id, req, userId, ct)); }
            catch (KeyNotFoundException e) { return Results.NotFound(new { error = e.Message }); }
            catch (InvalidOperationException e) { return Results.BadRequest(new { error = e.Message }); }
        });

        group.MapPut("/{id:guid}/steps/{stepId:guid}", async (Guid id, Guid stepId, UpdateSequenceStepRequest req, UpdateSequenceStep useCase, ClaimsPrincipal user, CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();
            try { return Results.Ok(await useCase.Handle(id, stepId, req, userId, ct)); }
            catch (KeyNotFoundException e) { return Results.NotFound(new { error = e.Message }); }
            catch (InvalidOperationException e) { return Results.BadRequest(new { error = e.Message }); }
        });

        group.MapPut("/{id:guid}/steps/{stepId:guid}/content", async (Guid id, Guid stepId, UpdateSequenceStepContentRequest req, UpdateSequenceStepContent useCase, ClaimsPrincipal user, CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();
            try { return Results.Ok(await useCase.Handle(id, stepId, req, userId, ct)); }
            catch (KeyNotFoundException e) { return Results.NotFound(new { error = e.Message }); }
            catch (InvalidOperationException e) { return Results.BadRequest(new { error = e.Message }); }
        });

        group.MapDelete("/{id:guid}/steps/{stepId:guid}", async (Guid id, Guid stepId, DeleteSequenceStep useCase, ClaimsPrincipal user, CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();
            try { await useCase.Handle(id, stepId, userId, ct); return Results.NoContent(); }
            catch (KeyNotFoundException e) { return Results.NotFound(new { error = e.Message }); }
            catch (InvalidOperationException e) { return Results.BadRequest(new { error = e.Message }); }
        });

        group.MapPut("/{id:guid}/steps/reorder", async (Guid id, ReorderSequenceStepsRequest req, ReorderSequenceSteps useCase, ClaimsPrincipal user, CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();
            try { await useCase.Handle(id, req, userId, ct); return Results.NoContent(); }
            catch (KeyNotFoundException e) { return Results.NotFound(new { error = e.Message }); }
            catch (ArgumentException e) { return Results.BadRequest(new { error = e.Message }); }
            catch (InvalidOperationException e) { return Results.BadRequest(new { error = e.Message }); }
        });

        // Prospect Enrollment
        group.MapPost("/{id:guid}/prospects", async (Guid id, EnrollProspectRequest req, EnrollProspect useCase, ClaimsPrincipal user, CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();
            try { return Results.Ok(await useCase.Handle(id, req, userId, ct)); }
            catch (KeyNotFoundException e) { return Results.NotFound(new { error = e.Message }); }
            catch (InvalidOperationException e) { return Results.BadRequest(new { error = e.Message }); }
        });

        group.MapDelete("/{id:guid}/prospects/{prospectId:guid}", async (Guid id, Guid prospectId, RemoveProspect useCase, ClaimsPrincipal user, CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();
            try { await useCase.Handle(id, prospectId, userId, ct); return Results.NoContent(); }
            catch (KeyNotFoundException e) { return Results.NotFound(new { error = e.Message }); }
            catch (InvalidOperationException e) { return Results.BadRequest(new { error = e.Message }); }
        });

        // Execution Control
        group.MapPost("/{id:guid}/activate", async (Guid id, ActivateSequence useCase, ClaimsPrincipal user, CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();
            try { await useCase.Handle(id, userId, ct); return Results.Ok(); }
            catch (KeyNotFoundException e) { return Results.NotFound(new { error = e.Message }); }
            catch (InvalidOperationException e) { return Results.BadRequest(new { error = e.Message }); }
        });

        group.MapPost("/{id:guid}/pause", async (Guid id, PauseSequence useCase, ClaimsPrincipal user, CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();
            try { await useCase.Handle(id, userId, ct); return Results.Ok(); }
            catch (KeyNotFoundException e) { return Results.NotFound(new { error = e.Message }); }
            catch (InvalidOperationException e) { return Results.BadRequest(new { error = e.Message }); }
        });

        group.MapPost("/{id:guid}/cancel", async (Guid id, CancelSequence useCase, ClaimsPrincipal user, CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();
            try { await useCase.Handle(id, userId, ct); return Results.Ok(); }
            catch (KeyNotFoundException e) { return Results.NotFound(new { error = e.Message }); }
            catch (InvalidOperationException e) { return Results.BadRequest(new { error = e.Message }); }
        });

        group.MapPost("/{id:guid}/steps/{stepId:guid}/generate", async (Guid id, Guid stepId, GenerateStepContent useCase, ClaimsPrincipal user, CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();
            try { return Results.Ok(await useCase.Handle(id, stepId, userId, ct)); }
            catch (KeyNotFoundException e) { return Results.NotFound(new { error = e.Message }); }
            catch (InvalidOperationException e) { return Results.BadRequest(new { error = e.Message }); }
        });
    }
}
