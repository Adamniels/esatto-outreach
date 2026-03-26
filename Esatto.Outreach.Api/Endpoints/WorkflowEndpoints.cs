using System.Security.Claims;
using Esatto.Outreach.Application.DTOs;
using Esatto.Outreach.Application.DTOs.Prospects;
using Esatto.Outreach.Application.DTOs.Auth;
using Esatto.Outreach.Application.DTOs.Intelligence;
using Esatto.Outreach.Application.DTOs.Outreach;
using Esatto.Outreach.Application.DTOs.Webhooks;
using Esatto.Outreach.Application.DTOs.Workflows;
using Esatto.Outreach.Application.UseCases.Workflows;
using Esatto.Outreach.Domain.Entities;
using Esatto.Outreach.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace Esatto.Outreach.Api.Endpoints;

public static class WorkflowEndpoints
{
    public static void MapWorkflowEndpoints(this WebApplication app)
    {
        // ============ TEMPLATES ============
        
        app.MapGet("/workflow-templates", async (WorkflowTemplateService service, CancellationToken ct) =>
        {
            var templates = await service.GetAllTemplatesAsync(ct);
            var dtos = templates.Select(t => new WorkflowTemplateDto(
                t.Id,
                t.Name,
                t.Description,
                t.IsDefault,
                t.Steps.Select(s => new WorkflowTemplateStepDto(s.StepType, s.DayOffset, $"{(int)s.TimeOfDay.TotalHours:D2}:{s.TimeOfDay.Minutes:D2}", s.GenerationStrategy)).ToList()
            )).ToList();
            
            return Results.Ok(dtos);
        })
        .RequireAuthorization();

        app.MapGet("/workflow-templates/{id:guid}", async (Guid id, WorkflowTemplateService service, CancellationToken ct) =>
        {
            var t = await service.GetTemplateAsync(id, ct);
            if (t is null) return Results.NotFound();

            var dto = new WorkflowTemplateDto(
                t.Id,
                t.Name,
                t.Description,
                t.IsDefault,
                t.Steps.Select(s => new WorkflowTemplateStepDto(s.StepType, s.DayOffset, $"{(int)s.TimeOfDay.TotalHours:D2}:{s.TimeOfDay.Minutes:D2}", s.GenerationStrategy)).ToList()
            );

            return Results.Ok(dto);
        })
        .RequireAuthorization();

        app.MapPost("/workflow-templates", async (CreateWorkflowTemplateRequest req, WorkflowTemplateService service, CancellationToken ct) =>
        {
            var serviceSteps = req.Steps?.Select(s => new Esatto.Outreach.Application.UseCases.Workflows.WorkflowTemplateStepDTO(s.Type, s.DayOffset, TimeSpan.Parse(s.TimeOfDay), s.GenerationStrategy)).ToList();

            var created = await service.CreateTemplateAsync(req.Name, req.Description, serviceSteps, ct);
            
            var dto = new WorkflowTemplateDto(
                created.Id,
                created.Name,
                created.Description,
                created.IsDefault,
                created.Steps.Select(s => new WorkflowTemplateStepDto(s.StepType, s.DayOffset, $"{(int)s.TimeOfDay.TotalHours:D2}:{s.TimeOfDay.Minutes:D2}", s.GenerationStrategy)).ToList()
            );

            return Results.Created($"/workflow-templates/{created.Id}", dto);
        })
        .RequireAuthorization();

        app.MapPut("/workflow-templates/{id:guid}", async (Guid id, UpdateWorkflowTemplateRequest req, WorkflowTemplateService service, CancellationToken ct) =>
        {
            var serviceSteps = req.Steps?.Select(s => new Esatto.Outreach.Application.UseCases.Workflows.WorkflowTemplateStepDTO(s.Type, s.DayOffset, TimeSpan.Parse(s.TimeOfDay), s.GenerationStrategy)).ToList()  
                ?? new List<Esatto.Outreach.Application.UseCases.Workflows.WorkflowTemplateStepDTO>();

            await service.UpdateTemplateAsync(id, req.Name, req.Description, serviceSteps, ct);
            return Results.Ok();
        })
        .RequireAuthorization();

        app.MapDelete("/workflow-templates/{id:guid}", async (Guid id, WorkflowTemplateService service, CancellationToken ct) =>
        {
            await service.DeleteTemplateAsync(id, ct);
            return Results.NoContent();
        })
        .RequireAuthorization();

        app.MapPost("/workflow-templates/{id:guid}/default", async (Guid id, WorkflowTemplateService service, CancellationToken ct) =>
        {
            await service.SetDefaultAsync(id, ct);
            return Results.Ok();
        })
        .RequireAuthorization();
        
        app.MapPut("/workflow-template-steps/{id:guid}", async (Guid id, UpdateTemplateStepRequest req, WorkflowTemplateService service, CancellationToken ct) =>
        {
            var timeOfDay = TimeSpan.Parse(req.TimeOfDay);
            await service.UpdateTemplateStepAsync(id, req.Type, req.DayOffset, timeOfDay, req.GenerationStrategy, ct);
            return Results.Ok();
        })
        .RequireAuthorization();


        // ============ INSTANCES ============

        app.MapGet("/prospects/{prospectId:guid}/workflows", async (Guid prospectId, WorkflowInstanceService service, CancellationToken ct) =>
        {
            var instances = await service.GetInstancesForProspectAsync(prospectId, ct);
            return Results.Ok(instances.Select(ToDto));
        })
        .RequireAuthorization();

        app.MapPost("/prospects/{prospectId:guid}/workflows", async (
            Guid prospectId, 
            [FromBody] Guid templateId, 
            WorkflowInstanceService service, 
            ClaimsPrincipal user, 
            CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

            try 
            {
                var instance = await service.CreateInstanceFromTemplateAsync(prospectId, templateId, userId, ct);
                return Results.Created($"/workflow-instances/{instance.Id}", ToDto(instance));
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

        app.MapDelete("/prospects/{prospectId:guid}/workflow", async (Guid prospectId, WorkflowInstanceService service, CancellationToken ct) =>
        {
            await service.DeleteWorkflowAsync(prospectId, ct);
            return Results.NoContent();
        })
        .RequireAuthorization();

        app.MapGet("/workflow-instances/{id:guid}/can-activate", async (Guid id, WorkflowInstanceService service, CancellationToken ct) =>
        {
            try
            {
                var errors = await service.ValidateCanActivateAsync(id, ct);
                return Results.Ok(new { canActivate = errors.Count == 0, errors });
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound();
            }
        })
        .RequireAuthorization();
        
        app.MapPost("/workflow-instances/{id:guid}/activate", async (Guid id, [FromBody] ActivateWorkflowRequest req, WorkflowInstanceService service, CancellationToken ct) =>
        {
            try 
            {
                // Validate before activation
                var errors = await service.ValidateCanActivateAsync(id, ct);
                if (errors.Any())
                {
                    return Results.BadRequest(new { error = "Cannot activate workflow", validationErrors = errors });
                }
                
                await service.ActivateAsync(id, req.TimeZoneId, ct);
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
        
        app.MapPost("/workflow-steps/{id:guid}/regenerate-draft", async (Guid id, WorkflowInstanceService service, ClaimsPrincipal user, CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();
            
            await service.RegenerateDraftAsync(id, userId, ct);
             // Return updated step? 
             // We can fetch it
            return Results.Ok();
        })
        .RequireAuthorization();

        // Step Management
        app.MapPost("/workflow-instances/{id:guid}/steps", async (Guid id, [FromBody] AddStepRequest req, WorkflowInstanceService service, ClaimsPrincipal user, CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

            try 
            {
                var instance = await service.AddStepToInstanceAsync(id, req.Type, req.DayOffset, TimeSpan.Parse(req.TimeOfDay), req.GenerationStrategy, userId, ct);
                return Results.Ok(ToDto(instance));
            }
            catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
        })
        .RequireAuthorization();

        app.MapDelete("/workflow-instances/{instanceId:guid}/steps/{stepId:guid}", async (Guid instanceId, Guid stepId, WorkflowInstanceService service, CancellationToken ct) =>
        {
            try
            {
                await service.DeleteStepAsync(instanceId, stepId, ct);
                return Results.Ok();
            }
            catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
        })
        .RequireAuthorization();

        app.MapPatch("/workflow-steps/{id:guid}/content", async (Guid id, [FromBody] UpdateStepContentRequest req, WorkflowInstanceService service, CancellationToken ct) =>
        {
            try
            {
                await service.UpdateStepContentAsync(id, req.Subject, req.Body, ct);
                return Results.Ok();
            }
            catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
        })
        .RequireAuthorization();

        app.MapPut("/workflow-steps/{id:guid}", async (Guid id, [FromBody] UpdateStepConfigRequest req, WorkflowInstanceService service, CancellationToken ct) =>
        {
            try
            {
                await service.UpdateStepConfigurationAsync(id, req.Type, req.DayOffset, TimeSpan.Parse(req.TimeOfDay), req.GenerationStrategy, ct);
                return Results.Ok();
            }
            catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
        })
        .RequireAuthorization();
    }

    private static WorkflowInstanceDto ToDto(WorkflowInstance entity)
    {
        return new WorkflowInstanceDto(
            entity.Id,
            entity.ProspectId,
            entity.Status,
            entity.CreatedAt,
            entity.StartedAt,
            entity.CompletedAt,
            entity.Steps.Select(s => new WorkflowStepDto(
                s.Id,
                s.Type,
                s.OrderIndex,
                s.DayOffset,
                $"{(int)s.TimeOfDay.TotalHours:D2}:{s.TimeOfDay.Minutes:D2}",
                s.GenerationStrategy,
                s.RunAt,
                s.Status,
                s.EmailSubject,
                s.BodyContent,
                s.FailureReason,
                s.RetryCount
            )).OrderBy(s => s.OrderIndex).ToList()
        );
    }
}

public record AddStepRequest(WorkflowStepType Type, int DayOffset, string TimeOfDay, ContentGenerationStrategy? GenerationStrategy);
public record UpdateStepContentRequest(string? Subject, string? Body);
public record UpdateStepConfigRequest(WorkflowStepType Type, int DayOffset, string TimeOfDay, ContentGenerationStrategy? GenerationStrategy);
public record UpdateTemplateStepRequest(WorkflowStepType Type, int DayOffset, string TimeOfDay, ContentGenerationStrategy? GenerationStrategy);
public record ActivateWorkflowRequest(string TimeZoneId = "UTC");
