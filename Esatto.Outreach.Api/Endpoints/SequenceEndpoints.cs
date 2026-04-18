using System.Security.Claims;
using Esatto.Outreach.Application.Features.Sequences.ActivateSequence;
using Esatto.Outreach.Application.Features.Sequences.AddSequenceStep;
using Esatto.Outreach.Application.Features.Sequences.CancelSequence;
using Esatto.Outreach.Application.Features.Sequences.CompleteSequenceSetup;
using Esatto.Outreach.Application.Features.Sequences.CreateSequence;
using Esatto.Outreach.Application.Features.Sequences.DeleteSequence;
using Esatto.Outreach.Application.Features.Sequences.DeleteSequenceStep;
using Esatto.Outreach.Application.Features.Sequences.EnrollProspect;
using Esatto.Outreach.Application.Features.Sequences.GenerateStepContent;
using Esatto.Outreach.Application.Features.Sequences.GetSequence;
using Esatto.Outreach.Application.Features.Sequences.ListSequences;
using Esatto.Outreach.Application.Features.Sequences.PauseSequence;
using Esatto.Outreach.Application.Features.Sequences.RemoveProspect;
using Esatto.Outreach.Application.Features.Sequences.ReorderSequenceSteps;
using Esatto.Outreach.Application.Features.Sequences.SaveBuilderProgress;
using Esatto.Outreach.Application.Features.Sequences.UpdateSequence;
using Esatto.Outreach.Application.Features.Sequences.UpdateSequenceStep;
using Esatto.Outreach.Application.Features.Sequences.UpdateSequenceStepContent;

namespace Esatto.Outreach.Api.Endpoints;

public static class SequenceEndpoints
{
    public static void MapSequenceEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/sequences")
                       .RequireAuthorization();

        // Sequences CRUD
        group.MapPost("/", async (CreateSequenceRequest req, CreateSequenceCommandHandler useCase, ClaimsPrincipal user, CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();
            try { return Results.Ok(await useCase.Handle(req, userId, ct)); }
            catch (ArgumentException e) { return Results.BadRequest(new { error = e.Message }); }
        });

        group.MapGet("/", async (ListSequencesQueryHandler useCase, ClaimsPrincipal user, CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();
            return Results.Ok(await useCase.Handle(userId, ct));
        });

        group.MapGet("/{id:guid}", async (Guid id, GetSequenceQueryHandler useCase, ClaimsPrincipal user, CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();
            try { return Results.Ok(await useCase.Handle(id, userId, ct)); }
            catch (KeyNotFoundException e) { return Results.NotFound(new { error = e.Message }); }
        });

        group.MapPut("/{id:guid}", async (Guid id, UpdateSequenceRequest req, UpdateSequenceCommandHandler useCase, ClaimsPrincipal user, CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();
            try { return Results.Ok(await useCase.Handle(id, req, userId, ct)); }
            catch (KeyNotFoundException e) { return Results.NotFound(new { error = e.Message }); }
            catch (ArgumentException e) { return Results.BadRequest(new { error = e.Message }); }
        });

        group.MapDelete("/{id:guid}", async (Guid id, DeleteSequenceCommandHandler useCase, ClaimsPrincipal user, CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();
            try { await useCase.Handle(id, userId, ct); return Results.NoContent(); }
            catch (KeyNotFoundException e) { return Results.NotFound(new { error = e.Message }); }
            catch (InvalidOperationException e) { return Results.BadRequest(new { error = e.Message }); }
        });

        // Step Management
        group.MapPost("/{id:guid}/steps", async (Guid id, AddSequenceStepRequest req, AddSequenceStepCommandHandler useCase, ClaimsPrincipal user, CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();
            try { return Results.Ok(await useCase.Handle(id, req, userId, ct)); }
            catch (KeyNotFoundException e) { return Results.NotFound(new { error = e.Message }); }
            catch (ArgumentException e) { return Results.BadRequest(new { error = e.Message }); }
            catch (InvalidOperationException e) { return Results.BadRequest(new { error = e.Message }); }
        });

        group.MapPut("/{id:guid}/steps/{stepId:guid}", async (Guid id, Guid stepId, UpdateSequenceStepRequest req, UpdateSequenceStepCommandHandler useCase, ClaimsPrincipal user, CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();
            try { return Results.Ok(await useCase.Handle(id, stepId, req, userId, ct)); }
            catch (KeyNotFoundException e) { return Results.NotFound(new { error = e.Message }); }
            catch (ArgumentException e) { return Results.BadRequest(new { error = e.Message }); }
            catch (InvalidOperationException e) { return Results.BadRequest(new { error = e.Message }); }
        });

        group.MapPut("/{id:guid}/steps/{stepId:guid}/content", async (Guid id, Guid stepId, UpdateSequenceStepContentRequest req, UpdateSequenceStepContentCommandHandler useCase, ClaimsPrincipal user, CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();
            try { return Results.Ok(await useCase.Handle(id, stepId, req, userId, ct)); }
            catch (KeyNotFoundException e) { return Results.NotFound(new { error = e.Message }); }
            catch (ArgumentException e) { return Results.BadRequest(new { error = e.Message }); }
            catch (InvalidOperationException e) { return Results.BadRequest(new { error = e.Message }); }
        });

        group.MapDelete("/{id:guid}/steps/{stepId:guid}", async (Guid id, Guid stepId, DeleteSequenceStepCommandHandler useCase, ClaimsPrincipal user, CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();
            try { await useCase.Handle(id, stepId, userId, ct); return Results.NoContent(); }
            catch (KeyNotFoundException e) { return Results.NotFound(new { error = e.Message }); }
            catch (ArgumentException e) { return Results.BadRequest(new { error = e.Message }); }
            catch (InvalidOperationException e) { return Results.BadRequest(new { error = e.Message }); }
        });

        group.MapPut("/{id:guid}/steps/reorder", async (Guid id, ReorderSequenceStepsRequest req, ReorderSequenceStepsCommandHandler useCase, ClaimsPrincipal user, CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();
            try { await useCase.Handle(id, req, userId, ct); return Results.NoContent(); }
            catch (KeyNotFoundException e) { return Results.NotFound(new { error = e.Message }); }
            catch (ArgumentException e) { return Results.BadRequest(new { error = e.Message }); }
            catch (InvalidOperationException e) { return Results.BadRequest(new { error = e.Message }); }
        });

        // Prospect Enrollment
        group.MapPost("/{id:guid}/prospects", async (Guid id, EnrollProspectRequest req, EnrollProspectCommandHandler useCase, ClaimsPrincipal user, CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();
            try { return Results.Ok(await useCase.Handle(id, req, userId, ct)); }
            catch (KeyNotFoundException e) { return Results.NotFound(new { error = e.Message }); }
            catch (InvalidOperationException e) { return Results.BadRequest(new { error = e.Message }); }
        });

        group.MapDelete("/{id:guid}/prospects/{sequenceProspectId:guid}", async (Guid id, Guid sequenceProspectId, RemoveProspectCommandHandler useCase, ClaimsPrincipal user, CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();
            try { await useCase.Handle(id, sequenceProspectId, userId, ct); return Results.NoContent(); }
            catch (KeyNotFoundException e) { return Results.NotFound(new { error = e.Message }); }
            catch (InvalidOperationException e) { return Results.BadRequest(new { error = e.Message }); }
        });

        group.MapPut("/{id:guid}/progress", async (Guid id, SaveBuilderProgressRequest req, SaveBuilderProgressCommandHandler useCase, ClaimsPrincipal user, CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();
            try { return Results.Ok(await useCase.Handle(id, req, userId, ct)); }
            catch (KeyNotFoundException e) { return Results.NotFound(new { error = e.Message }); }
            catch (InvalidOperationException e) { return Results.BadRequest(new { error = e.Message }); }
        });

        group.MapPost("/{id:guid}/complete-setup", async (Guid id, CompleteSequenceSetupCommandHandler useCase, ClaimsPrincipal user, CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();
            try { return Results.Ok(await useCase.Handle(id, userId, ct)); }
            catch (KeyNotFoundException e) { return Results.NotFound(new { error = e.Message }); }
            catch (InvalidOperationException e) { return Results.BadRequest(new { error = e.Message }); }
        });

        // Execution Control
        group.MapPost("/{id:guid}/activate", async (Guid id, ActivateSequenceCommandHandler useCase, ClaimsPrincipal user, CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();
            try { await useCase.Handle(id, userId, ct); return Results.Ok(); }
            catch (KeyNotFoundException e) { return Results.NotFound(new { error = e.Message }); }
            catch (InvalidOperationException e) { return Results.BadRequest(new { error = e.Message }); }
        });

        group.MapPost("/{id:guid}/pause", async (Guid id, PauseSequenceCommandHandler useCase, ClaimsPrincipal user, CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();
            try { await useCase.Handle(id, userId, ct); return Results.Ok(); }
            catch (KeyNotFoundException e) { return Results.NotFound(new { error = e.Message }); }
            catch (InvalidOperationException e) { return Results.BadRequest(new { error = e.Message }); }
        });

        group.MapPost("/{id:guid}/cancel", async (Guid id, CancelSequenceCommandHandler useCase, ClaimsPrincipal user, CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();
            try { await useCase.Handle(id, userId, ct); return Results.Ok(); }
            catch (KeyNotFoundException e) { return Results.NotFound(new { error = e.Message }); }
            catch (InvalidOperationException e) { return Results.BadRequest(new { error = e.Message }); }
        });

        group.MapPost("/{id:guid}/steps/{stepId:guid}/generate", async (Guid id, Guid stepId, GenerateStepContentCommandHandler useCase, ClaimsPrincipal user, CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();
            try { return Results.Ok(await useCase.Handle(id, stepId, userId, ct)); }
            catch (KeyNotFoundException e) { return Results.NotFound(new { error = e.Message }); }
            catch (ArgumentException e) { return Results.BadRequest(new { error = e.Message }); }
            catch (InvalidOperationException e) { return Results.BadRequest(new { error = e.Message }); }
        });
    }
}
