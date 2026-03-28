using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Application.Abstractions.Services;
using Esatto.Outreach.Domain.Entities;
using Esatto.Outreach.Domain.Enums;
using Esatto.Outreach.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace Esatto.Outreach.Application.Services;

/// <summary>
/// Internal service for executing workflow steps via background workers.
/// Not exposed directly via endpoints.
/// </summary>
public class WorkflowStepExecutor
{
    private readonly IWorkflowRepository _repo;
    private readonly IEmailSender _emailSender;
    private readonly ILinkedInActionsClient _linkedInClient;
    private readonly ILogger<WorkflowStepExecutor> _logger;

    public WorkflowStepExecutor(
        IWorkflowRepository repo,
        IEmailSender emailSender,
        ILinkedInActionsClient linkedInClient,
        ILogger<WorkflowStepExecutor> logger)
    {
        _repo = repo;
        _emailSender = emailSender;
        _linkedInClient = linkedInClient;
        _logger = logger;
    }

    public async Task ExecuteStepAsync(Guid stepId, CancellationToken ct)
    {
        var step = await _repo.GetStepByIdAsync(stepId, ct);
        if (step == null) return;
        
        if (step.Status != WorkflowStepStatus.Pending) return;

        // CLAIM
        try 
        {
            step.MarkExecuting();
            await _repo.UpdateStepAsync(step, ct);
        }
        catch (DomainConcurrencyException)
        {
            _logger.LogInformation("Step {StepId} was claimed by another worker.", stepId);
            return;
        }

        // PERFORM INTERACTION
        try
        {
            await PerformActionAsync(step, ct);
            step.MarkSucceeded();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing step {StepId}", stepId);
            step.MarkFailed(ex.Message);
        }

        // SAVE FINAL STATE
        await _repo.UpdateStepAsync(step, ct);
        
        // CHECK COMPLETION
        var instance = await _repo.GetInstanceByIdAsync(step.WorkflowInstanceId, ct);
        if (instance != null)
        {
             var allSteps = instance.Steps;
             var anyPendingOrFailed = allSteps.Any(s => s.Status == WorkflowStepStatus.Pending || s.Status == WorkflowStepStatus.Executing || s.Status == WorkflowStepStatus.Failed);
             if (!anyPendingOrFailed)
             {
                 instance.Complete();
                 await _repo.UpdateInstanceAsync(instance, ct);
             }
        }
    }

    private async Task PerformActionAsync(WorkflowStep step, CancellationToken ct)
    {
        var instance = await _repo.GetInstanceByIdAsync(step.WorkflowInstanceId, ct);
        if (instance == null || instance.Prospect == null) throw new Exception("Instance or Prospect not found");
        
        var contact = instance.Prospect.GetActiveContact();
        if (contact == null) throw new Exception("No active contact for prospect");

        switch (step.Type)
        {
            case WorkflowStepType.Email:
                if (string.IsNullOrWhiteSpace(contact.Email)) throw new Exception("Contact has no email address");
                await _emailSender.SendEmailAsync(
                    contact.Email, 
                    step.EmailSubject ?? "No Subject", 
                    step.BodyContent ?? "", 
                    ct);
                break;

            case WorkflowStepType.LinkedInMessage:
                await _linkedInClient.SendMessageAsync(
                    "http://linkedin.com/placeholder", 
                    step.BodyContent ?? "", 
                    ct);
                break;
                
            default:
                await _linkedInClient.PerformInteractionAsync("http://linkedin.com/placeholder", step.Type.ToString(), ct);
                break;
        }
    }
}
