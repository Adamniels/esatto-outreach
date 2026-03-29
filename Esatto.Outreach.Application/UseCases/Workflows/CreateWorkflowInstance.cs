using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Application.Services;
using Esatto.Outreach.Domain.Entities;
using Esatto.Outreach.Domain.Enums;

namespace Esatto.Outreach.Application.UseCases.Workflows;

public class CreateWorkflowInstance
{
    private readonly IWorkflowRepository _repo;
    private readonly IProspectRepository _prospectRepo;
    private readonly WorkflowDraftGenerator _draftGenerator;

    public CreateWorkflowInstance(IWorkflowRepository repo, IProspectRepository prospectRepo, WorkflowDraftGenerator draftGenerator)
    {
        _repo = repo;
        _prospectRepo = prospectRepo;
        _draftGenerator = draftGenerator;
    }

    public async Task<WorkflowInstance> Handle(Guid prospectId, Guid templateId, string userId, CancellationToken ct = default)
    {
        // Enforce Single Workflow Rule
        var existing = await _repo.GetInstancesByProspectIdAsync(prospectId, ct);
        if (existing.Any())
            throw new InvalidOperationException("Prospect already has a workflow. Delete it first.");

        var template = await _repo.GetTemplateByIdAsync(templateId, ct)
            ?? throw new ArgumentException("Template not found");
        
        var instance = WorkflowInstance.Create(prospectId);
        
        foreach (var tStep in template.Steps.OrderBy(s => s.OrderIndex))
        {
            instance.AddStep(tStep.StepType, tStep.DayOffset, tStep.TimeOfDay, tStep.GenerationStrategy);
        }

        // Validate Dependencies
        await ValidateDependenciesAsync(instance, userId, ct);

        // Generate Drafts for created steps
        foreach (var step in instance.Steps)
        {
            if (step.Type == WorkflowStepType.Email || step.Type == WorkflowStepType.LinkedInMessage)
            {
                await _draftGenerator.GenerateDraftForStepAsync(step, prospectId, userId, ct);
            }
        }

        await _repo.AddInstanceAsync(instance, ct);
        return instance;
    }

    private async Task ValidateDependenciesAsync(WorkflowInstance instance, string userId, CancellationToken ct)
    {
        var prospect = await _prospectRepo.GetByIdAsync(instance.ProspectId, ct)
            ?? throw new InvalidOperationException("Prospect not found");

        bool hasContact = prospect.GetActiveContact() != null;
        bool hasIntelligence = prospect.EntityIntelligenceId != null;

        foreach (var step in instance.Steps)
        {
            if (step.Type == WorkflowStepType.Email || step.Type == WorkflowStepType.LinkedInMessage)
            {
                if (!hasContact)
                    throw new InvalidOperationException("Cannot create workflow: One or more steps require an active contact person. Please add and activate a contact first.");
                
                if (step.GenerationStrategy == ContentGenerationStrategy.UseCollectedData && !hasIntelligence)
                    throw new InvalidOperationException("Cannot create workflow: One or more steps require Collected Data (Enrichment), which is missing. Please enrich the prospect first.");
            }
        }
    }
}
