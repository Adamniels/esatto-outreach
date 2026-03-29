using System.Security.Claims;
using Esatto.Outreach.Application.DTOs.Workflows;
using Esatto.Outreach.Application.UseCases.Workflows;
using Esatto.Outreach.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace Esatto.Outreach.Api.Endpoints;

public static class WorkflowEndpoints
{
    public static void MapWorkflowEndpoints(this WebApplication app)
    {
        // ============ TEMPLATES ============
        
        app.MapGet("/workflow-templates", async (ListWorkflowTemplates useCase, CancellationToken ct) =>
        {
            var templates = await useCase.Handle(ct);
            var dtos = templates.Select(ToTemplateDto).ToList();
            return Results.Ok(dtos);
        })
        .RequireAuthorization();

        app.MapGet("/workflow-templates/{id:guid}", async (Guid id, GetWorkflowTemplate useCase, CancellationToken ct) =>
        {
            var t = await useCase.Handle(id, ct);
            if (t is null) return Results.NotFound();
            return Results.Ok(ToTemplateDto(t));
        })
        .RequireAuthorization();

        app.MapPost("/workflow-templates", async (CreateWorkflowTemplateRequest req, CreateWorkflowTemplate useCase, CancellationToken ct) =>
        {
            var steps = req.Steps?.Select(s => new WorkflowTemplateStepInputDto(s.Type, s.DayOffset, TimeSpan.Parse(s.TimeOfDay), s.GenerationStrategy)).ToList();
            var created = await useCase.Handle(req.Name, req.Description, steps, ct);
            return Results.Created($"/workflow-templates/{created.Id}", ToTemplateDto(created));
        })
        .RequireAuthorization();

        app.MapPut("/workflow-templates/{id:guid}", async (Guid id, UpdateWorkflowTemplateRequest req, UpdateWorkflowTemplate useCase, CancellationToken ct) =>
        {
            var steps = req.Steps?.Select(s => new WorkflowTemplateStepInputDto(s.Type, s.DayOffset, TimeSpan.Parse(s.TimeOfDay), s.GenerationStrategy)).ToList()  
                ?? new List<WorkflowTemplateStepInputDto>();
            await useCase.Handle(id, req.Name, req.Description, steps, ct);
            return Results.Ok();
        })
        .RequireAuthorization();

        app.MapDelete("/workflow-templates/{id:guid}", async (Guid id, DeleteWorkflowTemplate useCase, CancellationToken ct) =>
        {
            await useCase.Handle(id, ct);
            return Results.NoContent();
        })
        .RequireAuthorization();

        app.MapPost("/workflow-templates/{id:guid}/default", async (Guid id, SetDefaultWorkflowTemplate useCase, CancellationToken ct) =>
        {
            await useCase.Handle(id, ct);
            return Results.Ok();
        })
        .RequireAuthorization();
        
        app.MapPut("/workflow-template-steps/{id:guid}", async (Guid id, UpdateTemplateStepRequest req, UpdateWorkflowTemplateStep useCase, CancellationToken ct) =>
        {
            var timeOfDay = TimeSpan.Parse(req.TimeOfDay);
            await useCase.Handle(id, req.Type, req.DayOffset, timeOfDay, req.GenerationStrategy, ct);
            return Results.Ok();
        })
        .RequireAuthorization();


        // ============ INSTANCES ============

        app.MapGet("/prospects/{prospectId:guid}/workflows", async (Guid prospectId, ListWorkflowInstances useCase, CancellationToken ct) =>
        {
            var instances = await useCase.Handle(prospectId, ct);
            return Results.Ok(instances.Select(ToInstanceDto));
        })
        .RequireAuthorization();

        app.MapPost("/prospects/{prospectId:guid}/workflows", async (
            Guid prospectId, 
            [FromBody] Guid templateId, 
            CreateWorkflowInstance useCase, 
            ClaimsPrincipal user, 
            CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

            try 
            {
                var instance = await useCase.Handle(prospectId, templateId, userId, ct);
                return Results.Created($"/workflow-instances/{instance.Id}", ToInstanceDto(instance));
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("already has a workflow"))
            {
                return Results.Conflict(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        })
        .RequireAuthorization();

        app.MapDelete("/prospects/{prospectId:guid}/workflow", async (Guid prospectId, DeleteWorkflowInstance useCase, CancellationToken ct) =>
        {
            await useCase.Handle(prospectId, ct);
            return Results.NoContent();
        })
        .RequireAuthorization();

        app.MapGet("/workflow-instances/{id:guid}/can-activate", async (Guid id, ValidateWorkflowActivation useCase, CancellationToken ct) =>
        {
            try
            {
                var errors = await useCase.Handle(id, ct);
                return Results.Ok(new { canActivate = errors.Count == 0, errors });
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound();
            }
        })
        .RequireAuthorization();
        
        app.MapPost("/workflow-instances/{id:guid}/activate", async (Guid id, [FromBody] ActivateWorkflowRequest req, ActivateWorkflowInstance useCase, ValidateWorkflowActivation validateUseCase, CancellationToken ct) =>
        {
            try
            {
                // Validate before activation
                var errors = await validateUseCase.Handle(id, ct);
                if (errors.Any())
                {
                    return Results.BadRequest(new { error = "Cannot activate workflow", validationErrors = errors });
                }
                
                await useCase.Handle(id, req.TimeZoneId, ct);
                return Results.Ok();
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        })
        .RequireAuthorization();
        
        app.MapPost("/workflow-steps/{id:guid}/regenerate-draft", async (Guid id, RegenerateWorkflowStepDraft useCase, ClaimsPrincipal user, CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();
            
            await useCase.Handle(id, userId, ct);
            return Results.Ok();
        })
        .RequireAuthorization();

        // Step Management
        app.MapPost("/workflow-instances/{id:guid}/steps", async (Guid id, [FromBody] AddStepRequest req, AddWorkflowStep useCase, ClaimsPrincipal user, CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

            try 
            {
                var instance = await useCase.Handle(id, req.Type, req.DayOffset, TimeSpan.Parse(req.TimeOfDay), req.GenerationStrategy, userId, ct);
                return Results.Ok(ToInstanceDto(instance));
            }
            catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
        })
        .RequireAuthorization();

        app.MapDelete("/workflow-instances/{instanceId:guid}/steps/{stepId:guid}", async (Guid instanceId, Guid stepId, DeleteWorkflowStep useCase, CancellationToken ct) =>
        {
            try
            {
                await useCase.Handle(instanceId, stepId, ct);
                return Results.Ok();
            }
            catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
        })
        .RequireAuthorization();

        app.MapPatch("/workflow-steps/{id:guid}/content", async (Guid id, [FromBody] UpdateStepContentRequest req, UpdateWorkflowStepContent useCase, CancellationToken ct) =>
        {
            try
            {
                await useCase.Handle(id, req.Subject, req.Body, ct);
                return Results.Ok();
            }
            catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
        })
        .RequireAuthorization();

        app.MapPut("/workflow-steps/{id:guid}", async (Guid id, [FromBody] UpdateStepConfigRequest req, UpdateWorkflowStepConfig useCase, CancellationToken ct) =>
        {
            try
            {
                await useCase.Handle(id, req.Type, req.DayOffset, TimeSpan.Parse(req.TimeOfDay), req.GenerationStrategy, ct);
                return Results.Ok();
            }
            catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
        })
        .RequireAuthorization();
    }

    private static WorkflowTemplateDto ToTemplateDto(WorkflowTemplate t) => new(
        t.Id, t.Name, t.Description, t.IsDefault,
        t.Steps.Select(s => new WorkflowTemplateStepDto(s.StepType, s.DayOffset, $"{(int)s.TimeOfDay.TotalHours:D2}:{s.TimeOfDay.Minutes:D2}", s.GenerationStrategy)).ToList()
    );

    private static WorkflowInstanceDto ToInstanceDto(WorkflowInstance entity) => new(
        entity.Id, entity.ProspectId, entity.Status, entity.CreatedAt, entity.StartedAt, entity.CompletedAt,
        entity.Steps.Select(s => new WorkflowStepDto(
            s.Id, s.Type, s.OrderIndex, s.DayOffset,
            $"{(int)s.TimeOfDay.TotalHours:D2}:{s.TimeOfDay.Minutes:D2}",
            s.GenerationStrategy, s.RunAt, s.Status, s.EmailSubject, s.BodyContent, s.FailureReason, s.RetryCount
        )).OrderBy(s => s.OrderIndex).ToList()
    );
}
