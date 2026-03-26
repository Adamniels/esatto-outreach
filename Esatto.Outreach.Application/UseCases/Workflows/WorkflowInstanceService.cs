using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Application.Abstractions.Services;
using Esatto.Outreach.Application.Abstractions.Clients;
using Esatto.Outreach.Domain.Entities;
using Esatto.Outreach.Domain.Enums;

namespace Esatto.Outreach.Application.UseCases.Workflows;

public class WorkflowInstanceService
{
    private readonly IWorkflowRepository _repo;
    private readonly IProspectRepository _prospectRepo;
    private readonly GenerateDraftWorkflow _draftService;

    public WorkflowInstanceService(
        IWorkflowRepository repo, 
        IProspectRepository prospectRepo,
        GenerateDraftWorkflow draftService)
    {
        _repo = repo;
        _prospectRepo = prospectRepo;
        _draftService = draftService;
    }

    public async Task<WorkflowInstance> CreateInstanceFromTemplateAsync(Guid prospectId, Guid templateId, string userId, CancellationToken ct)
    {
        // Enforce Single Workflow Rule
        var existing = await _repo.GetInstancesByProspectIdAsync(prospectId, ct);
        if (existing.Any())
        {
            throw new InvalidOperationException("Prospect already has a workflow. Delete it first.");
        }

        var template = await _repo.GetTemplateByIdAsync(templateId, ct);
        if (template == null) throw new ArgumentException("Template not found");
        
        var instance = WorkflowInstance.Create(prospectId);
        
        foreach (var tStep in template.Steps.OrderBy(s => s.OrderIndex))
        {
            instance.AddStep(tStep.StepType, tStep.DayOffset, tStep.TimeOfDay, tStep.GenerationStrategy);
        }

        // Validate Dependencies *Before* Generation/Save
        await ValidateDependenciesAsync(instance, userId, ct);

        // Generate Drafts for created steps
        foreach (var step in instance.Steps)
        {
            if (step.Type == WorkflowStepType.Email || step.Type == WorkflowStepType.LinkedInMessage)
            {
                await _draftService.GenerateDraftForStepAsync(step, prospectId, userId, ct);
            }
        }

        await _repo.AddInstanceAsync(instance, ct);
        return instance;
    }

    public async Task ActivateAsync(Guid instanceId, string timeZoneId, CancellationToken ct)
    {
        var instance = await _repo.GetInstanceByIdAsync(instanceId, ct);
        if (instance == null) throw new KeyNotFoundException("Workflow instance not found");

        if (instance.Status != WorkflowStatus.Draft)
            throw new InvalidOperationException("Workflow is not in draft state");

        // Validate Active Contact
        var activeContact = instance.Prospect?.GetActiveContact();
        if (activeContact == null)
            throw new InvalidOperationException("Cannot activate workflow: Prospect has no active contact person.");
        
        // Validate other dependencies via Domain Logic
        var errors = instance.CanActivate(instance.Prospect?.EntityIntelligence != null);
        if (errors.Any())
             throw new InvalidOperationException($"Cannot activate workflow: {string.Join(", ", errors)}");

        instance.Activate(DateTime.UtcNow, timeZoneId);
        await _repo.UpdateInstanceAsync(instance, ct);
    }
    
    public async Task DeleteWorkflowAsync(Guid prospectId, CancellationToken ct)
    {
        var instances = await _repo.GetInstancesByProspectIdAsync(prospectId, ct);
        foreach (var instance in instances)
        {
            await _repo.DeleteInstanceAsync(instance, ct);
        }
    }
    
    public async Task<List<WorkflowInstance>> GetInstancesForProspectAsync(Guid prospectId, CancellationToken ct)
    {
        return await _repo.GetInstancesByProspectIdAsync(prospectId, ct);
    }
    
    
    public async Task RegenerateDraftAsync(Guid stepId, string userId, CancellationToken ct)
    {
        var step = await _repo.GetStepByIdAsync(stepId, ct);
        if (step == null) throw new KeyNotFoundException("Step not found");
        
        if (step.Status != WorkflowStepStatus.Pending && step.WorkflowInstance.Status != WorkflowStatus.Draft)
        {
             // Allow regenerating if Pending (scheduled but not run)
        }
        
        // Validate dependencies before regenerating?
        if (step.GenerationStrategy == ContentGenerationStrategy.UseCollectedData)
        {
             var prospect = await _prospectRepo.GetByIdAsync(step.WorkflowInstance.ProspectId, ct);
             if (prospect?.EntityIntelligenceId == null)
                 throw new InvalidOperationException("Cannot regenerate draft: Missing Entity Intelligence for 'UseCollectedData' strategy.");
        }
        
        await _draftService.GenerateDraftForStepAsync(step, step.WorkflowInstance.ProspectId, userId, ct);
        await _repo.UpdateStepAsync(step, ct);
    }

    public async Task<WorkflowInstance> AddStepToInstanceAsync(Guid instanceId, WorkflowStepType type, int dayOffset, TimeSpan timeOfDay, ContentGenerationStrategy? generationStrategy, string userId, CancellationToken ct)
    {
        var instance = await _repo.GetInstanceByIdAsync(instanceId, ct);
        if (instance == null) throw new KeyNotFoundException("Workflow instance not found");

        if (instance.Status != WorkflowStatus.Draft)
            throw new InvalidOperationException("Cannot add steps to non-draft workflow");

        instance.AddStep(type, dayOffset, timeOfDay, generationStrategy);
        var newStep = instance.Steps.Last();

        // Validate Dependencies immediately
        if (type == WorkflowStepType.Email || type == WorkflowStepType.LinkedInMessage)
        {
             var prospect = await _prospectRepo.GetByIdAsync(instance.ProspectId, ct);
             if (prospect == null) throw new InvalidOperationException("Prospect not found");

             if (prospect.GetActiveContact() == null)
                 throw new InvalidOperationException("Cannot add communication step: Prospect has no active contact person.");

             if (generationStrategy == ContentGenerationStrategy.UseCollectedData && prospect.EntityIntelligenceId == null)
                 throw new InvalidOperationException("Cannot add 'Use Collected Data' step: Prospect has no Entity Intelligence.");
        }

        // Generate draft immediately for new step
        if (newStep.Type == WorkflowStepType.Email || newStep.Type == WorkflowStepType.LinkedInMessage)
        {
            await _draftService.GenerateDraftForStepAsync(newStep, instance.ProspectId, userId, ct);
        }

        await _repo.AddStepAsync(newStep, ct);
        await _repo.UpdateInstanceAsync(instance, ct);
        return instance;
    }

    private async Task ValidateDependenciesAsync(WorkflowInstance instance, string userId, CancellationToken ct)
    {
        var prospect = await _prospectRepo.GetByIdAsync(instance.ProspectId, ct);
        if (prospect == null) throw new InvalidOperationException("Prospect not found");

        bool hasContact = prospect.GetActiveContact() != null;
        bool hasIntelligence = prospect.EntityIntelligenceId != null;

        foreach (var step in instance.Steps)
        {
            // Only validate dependencies for communication steps (Email/LinkedIn)
            if (step.Type == WorkflowStepType.Email || step.Type == WorkflowStepType.LinkedInMessage)
            {
                if (!hasContact)
                    throw new InvalidOperationException("Cannot create workflow: One or more steps require an active contact person. Please add and activate a contact first.");
                
                if (step.GenerationStrategy == ContentGenerationStrategy.UseCollectedData && !hasIntelligence)
                    throw new InvalidOperationException("Cannot create workflow: One or more steps require Collected Data (Enrichment), which is missing. Please enrich the prospect first.");
            }
        }
    }

    public async Task UpdateStepContentAsync(Guid stepId, string? subject, string? body, CancellationToken ct)
    {
        var step = await _repo.GetStepByIdAsync(stepId, ct);
        if (step == null) throw new KeyNotFoundException("Step not found");

        step.UpdateDraft(subject, body);
        await _repo.UpdateStepAsync(step, ct);
    }

    public async Task UpdateStepConfigurationAsync(Guid stepId, WorkflowStepType type, int dayOffset, TimeSpan timeOfDay, ContentGenerationStrategy? generationStrategy, CancellationToken ct)
    {
        var step = await _repo.GetStepByIdAsync(stepId, ct);
        if (step == null) throw new KeyNotFoundException("Step not found");
        
        // Get the parent instance to ensure reordering changes are saved
        var instance = await _repo.GetInstanceByIdAsync(step.WorkflowInstanceId, ct);
        if (instance == null) throw new KeyNotFoundException("Workflow instance not found");
        
        step.UpdateConfiguration(type, dayOffset, timeOfDay, generationStrategy);
        
        // UpdateConfiguration calls ReorderSteps() which modifies OrderIndex on all steps
        // So we need to update the entire instance to persist all changes
        await _repo.UpdateInstanceAsync(instance, ct);
    }

    public async Task DeleteStepAsync(Guid instanceId, Guid stepId, CancellationToken ct)
    {
        var instance = await _repo.GetInstanceByIdAsync(instanceId, ct);
        if (instance == null) throw new KeyNotFoundException("Workflow instance not found");

        if (instance.Status != WorkflowStatus.Draft)
             // Strict draft check? Or allow if pending?
             // Removing step from active workflow is dangerous for scheduling.
             // Let's enforce Draft for now as requested.
             throw new InvalidOperationException("Cannot delete steps from non-draft workflow");

        var stepToRemove = instance.Steps.FirstOrDefault(s => s.Id == stepId);
        if (stepToRemove == null) throw new KeyNotFoundException("Step not found in instance");

        // Use Domain Logic to remove and re-index
        instance.RemoveStep(stepId);
        
        // Explicitly delete from Repo?
        // Since we removed from collection, EF might just null FK or delete if cascade.
        // But we added DeleteStepAsync to Repo for clarity.
        // Domain 'RemoveStep' modifies the collection.
        // We need to tell Repo to delete it.
        await _repo.DeleteStepAsync(stepToRemove, ct);
        // And update instance for index changes
        await _repo.UpdateInstanceAsync(instance, ct);
    }
    
    /// <summary>
    /// Validates whether a workflow instance can be activated.
    /// Checks if all Email/LinkedIn steps have generation strategies,
    /// and if UseCollectedData steps have required Entity Intelligence.
    /// </summary>
    public async Task<List<string>> ValidateCanActivateAsync(Guid instanceId, CancellationToken ct)
    {
        var instance = await _repo.GetInstanceByIdAsync(instanceId, ct);
        if (instance == null) throw new KeyNotFoundException("Workflow instance not found");
        
        // Check if prospect has Entity Intelligence
        bool hasEntityIntelligence = instance.Prospect?.EntityIntelligence != null;
        
        // Use domain validation method
        return instance.CanActivate(hasEntityIntelligence);
    }
}
