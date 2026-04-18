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
        group.MapPost("/", async (CreateSequenceCommand cmd, CreateSequenceCommandHandler handler, ClaimsPrincipal user, CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();
            try { return Results.Ok(await handler.Handle(cmd, userId, ct)); }
            catch (ArgumentException e) { return Results.BadRequest(new { error = e.Message }); }
        });

        group.MapGet("/", async (ListSequencesQueryHandler handler, ClaimsPrincipal user, CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();
            return Results.Ok(await handler.Handle(new ListSequencesQuery(), userId, ct));
        });

        group.MapGet("/{id:guid}", async (Guid id, GetSequenceQueryHandler handler, ClaimsPrincipal user, CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();
            try { return Results.Ok(await handler.Handle(new GetSequenceQuery(id), userId, ct)); }
            catch (KeyNotFoundException e) { return Results.NotFound(new { error = e.Message }); }
        });

        group.MapPut("/{id:guid}", async (Guid id, UpdateSequenceCommand req, UpdateSequenceCommandHandler handler, ClaimsPrincipal user, CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();
            try { return Results.Ok(await handler.Handle(req with { Id = id }, userId, ct)); }
            catch (KeyNotFoundException e) { return Results.NotFound(new { error = e.Message }); }
            catch (ArgumentException e) { return Results.BadRequest(new { error = e.Message }); }
        });

        group.MapDelete("/{id:guid}", async (Guid id, DeleteSequenceCommandHandler handler, ClaimsPrincipal user, CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();
            try { await handler.Handle(new DeleteSequenceCommand(id), userId, ct); return Results.NoContent(); }
            catch (KeyNotFoundException e) { return Results.NotFound(new { error = e.Message }); }
            catch (InvalidOperationException e) { return Results.BadRequest(new { error = e.Message }); }
        });

        // Step Management
        group.MapPost("/{id:guid}/steps", async (Guid id, AddSequenceStepCommand req, AddSequenceStepCommandHandler handler, ClaimsPrincipal user, CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();
            try { return Results.Ok(await handler.Handle(req with { SequenceId = id }, userId, ct)); }
            catch (KeyNotFoundException e) { return Results.NotFound(new { error = e.Message }); }
            catch (ArgumentException e) { return Results.BadRequest(new { error = e.Message }); }
            catch (InvalidOperationException e) { return Results.BadRequest(new { error = e.Message }); }
        });

        group.MapPut("/{id:guid}/steps/{stepId:guid}", async (Guid id, Guid stepId, UpdateSequenceStepCommand req, UpdateSequenceStepCommandHandler handler, ClaimsPrincipal user, CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();
            try { return Results.Ok(await handler.Handle(req with { SequenceId = id, StepId = stepId }, userId, ct)); }
            catch (KeyNotFoundException e) { return Results.NotFound(new { error = e.Message }); }
            catch (ArgumentException e) { return Results.BadRequest(new { error = e.Message }); }
            catch (InvalidOperationException e) { return Results.BadRequest(new { error = e.Message }); }
        });

        group.MapPut("/{id:guid}/steps/{stepId:guid}/content", async (Guid id, Guid stepId, UpdateSequenceStepContentCommand req, UpdateSequenceStepContentCommandHandler handler, ClaimsPrincipal user, CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();
            try { return Results.Ok(await handler.Handle(req with { SequenceId = id, StepId = stepId }, userId, ct)); }
            catch (KeyNotFoundException e) { return Results.NotFound(new { error = e.Message }); }
            catch (ArgumentException e) { return Results.BadRequest(new { error = e.Message }); }
            catch (InvalidOperationException e) { return Results.BadRequest(new { error = e.Message }); }
        });

        group.MapDelete("/{id:guid}/steps/{stepId:guid}", async (Guid id, Guid stepId, DeleteSequenceStepCommandHandler handler, ClaimsPrincipal user, CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();
            try { await handler.Handle(new DeleteSequenceStepCommand(id, stepId), userId, ct); return Results.NoContent(); }
            catch (KeyNotFoundException e) { return Results.NotFound(new { error = e.Message }); }
            catch (ArgumentException e) { return Results.BadRequest(new { error = e.Message }); }
            catch (InvalidOperationException e) { return Results.BadRequest(new { error = e.Message }); }
        });

        group.MapPut("/{id:guid}/steps/reorder", async (Guid id, ReorderSequenceStepsCommand req, ReorderSequenceStepsCommandHandler handler, ClaimsPrincipal user, CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();
            try { await handler.Handle(req with { SequenceId = id }, userId, ct); return Results.NoContent(); }
            catch (KeyNotFoundException e) { return Results.NotFound(new { error = e.Message }); }
            catch (ArgumentException e) { return Results.BadRequest(new { error = e.Message }); }
            catch (InvalidOperationException e) { return Results.BadRequest(new { error = e.Message }); }
        });

        // Prospect Enrollment
        group.MapPost("/{id:guid}/prospects", async (Guid id, EnrollProspectCommand req, EnrollProspectCommandHandler handler, ClaimsPrincipal user, CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();
            try { return Results.Ok(await handler.Handle(req with { SequenceId = id }, userId, ct)); }
            catch (KeyNotFoundException e) { return Results.NotFound(new { error = e.Message }); }
            catch (InvalidOperationException e) { return Results.BadRequest(new { error = e.Message }); }
        });

        group.MapDelete("/{id:guid}/prospects/{sequenceProspectId:guid}", async (Guid id, Guid sequenceProspectId, RemoveProspectCommandHandler handler, ClaimsPrincipal user, CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();
            try { await handler.Handle(new RemoveProspectCommand(id, sequenceProspectId), userId, ct); return Results.NoContent(); }
            catch (KeyNotFoundException e) { return Results.NotFound(new { error = e.Message }); }
            catch (InvalidOperationException e) { return Results.BadRequest(new { error = e.Message }); }
        });

        group.MapPut("/{id:guid}/progress", async (Guid id, SaveBuilderProgressCommand req, SaveBuilderProgressCommandHandler handler, ClaimsPrincipal user, CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();
            try { return Results.Ok(await handler.Handle(req with { Id = id }, userId, ct)); }
            catch (KeyNotFoundException e) { return Results.NotFound(new { error = e.Message }); }
            catch (InvalidOperationException e) { return Results.BadRequest(new { error = e.Message }); }
        });

        group.MapPost("/{id:guid}/complete-setup", async (Guid id, CompleteSequenceSetupCommandHandler handler, ClaimsPrincipal user, CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();
            try { return Results.Ok(await handler.Handle(new CompleteSequenceSetupCommand(id), userId, ct)); }
            catch (KeyNotFoundException e) { return Results.NotFound(new { error = e.Message }); }
            catch (InvalidOperationException e) { return Results.BadRequest(new { error = e.Message }); }
        });

        // Execution Control
        group.MapPost("/{id:guid}/activate", async (Guid id, ActivateSequenceCommandHandler handler, ClaimsPrincipal user, CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();
            try { await handler.Handle(new ActivateSequenceCommand(id), userId, ct); return Results.Ok(); }
            catch (KeyNotFoundException e) { return Results.NotFound(new { error = e.Message }); }
            catch (InvalidOperationException e) { return Results.BadRequest(new { error = e.Message }); }
        });

        group.MapPost("/{id:guid}/pause", async (Guid id, PauseSequenceCommandHandler handler, ClaimsPrincipal user, CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();
            try { await handler.Handle(new PauseSequenceCommand(id), userId, ct); return Results.Ok(); }
            catch (KeyNotFoundException e) { return Results.NotFound(new { error = e.Message }); }
            catch (InvalidOperationException e) { return Results.BadRequest(new { error = e.Message }); }
        });

        group.MapPost("/{id:guid}/cancel", async (Guid id, CancelSequenceCommandHandler handler, ClaimsPrincipal user, CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();
            try { await handler.Handle(new CancelSequenceCommand(id), userId, ct); return Results.Ok(); }
            catch (KeyNotFoundException e) { return Results.NotFound(new { error = e.Message }); }
            catch (InvalidOperationException e) { return Results.BadRequest(new { error = e.Message }); }
        });

        group.MapPost("/{id:guid}/steps/{stepId:guid}/generate", async (Guid id, Guid stepId, GenerateStepContentCommandHandler handler, ClaimsPrincipal user, CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();
            try { return Results.Ok(await handler.Handle(new GenerateStepContentCommand(id, stepId), userId, ct)); }
            catch (KeyNotFoundException e) { return Results.NotFound(new { error = e.Message }); }
            catch (ArgumentException e) { return Results.BadRequest(new { error = e.Message }); }
            catch (InvalidOperationException e) { return Results.BadRequest(new { error = e.Message }); }
        });
    }
}
