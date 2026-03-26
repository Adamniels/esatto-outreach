using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Application.Abstractions.Services;
using Esatto.Outreach.Application.Abstractions.Clients;
using Esatto.Outreach.Domain.Entities;
using Esatto.Outreach.Domain.Enums;
using Esatto.Outreach.Domain.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore; // For exception types

namespace Esatto.Outreach.Application.UseCases.Workflows;

public class StepExecutionService
{
    private readonly IWorkflowRepository _repo;
    private readonly IEmailSender _emailSender;
    private readonly ILinkedInActionsClient _linkedInClient;
    private readonly ILogger<StepExecutionService> _logger;

    public StepExecutionService(
        IWorkflowRepository repo,
        IEmailSender emailSender,
        ILinkedInActionsClient linkedInClient,
        ILogger<StepExecutionService> logger)
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
        // Need to check other steps.
        // Repo `GetInstancesByProspectIdAsync` gets all for prospect.
        // Maybe I need `GetInstanceByIdAsync` to check its steps?
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
        // Hydrate data (Instance and Prospect are loaded via Repo GetStepByIdAsync Includes?
        // Let's check Repo implementation of GetStepByIdAsync.
        // It includes WorkflowInstance -> Prospect.
        // But does it include ContactPersons?
        // Repo: .Include(s => s.WorkflowInstance).ThenInclude(i => i.Prospect)
        // Does NOT include ContactPersons in GetStepByIdAsync default impl I wrote.
        // I should fix Repo or Load here.
        // I can use `GetInstanceByIdAsync` which DOES include ContactPersons.
        
        var instance = await _repo.GetInstanceByIdAsync(step.WorkflowInstanceId, ct);
        if (instance == null || instance.Prospect == null) throw new Exception("Instance or Prospect not found");
        
        // Ensure ContactPersons loaded (Repo GetInstanceByIdAsync includes them).
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
                 // Assuming Mock client handles url/logic
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
