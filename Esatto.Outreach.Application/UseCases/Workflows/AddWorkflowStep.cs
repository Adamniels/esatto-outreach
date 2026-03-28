using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Application.Services;
using Esatto.Outreach.Domain.Entities;
using Esatto.Outreach.Domain.Enums;

namespace Esatto.Outreach.Application.UseCases.Workflows;

public class AddWorkflowStep
{
    private readonly IWorkflowRepository _repo;
    private readonly IProspectRepository _prospectRepo;
    private readonly WorkflowDraftGenerator _draftGenerator;

    public AddWorkflowStep(IWorkflowRepository repo, IProspectRepository prospectRepo, WorkflowDraftGenerator draftGenerator)
    {
        _repo = repo;
        _prospectRepo = prospectRepo;
        _draftGenerator = draftGenerator;
    }

    public async Task<WorkflowInstance> Handle(Guid instanceId, WorkflowStepType type, int dayOffset, TimeSpan timeOfDay, ContentGenerationStrategy? generationStrategy, string userId, CancellationToken ct = default)
    {
        var instance = await _repo.GetInstanceByIdAsync(instanceId, ct)
            ?? throw new KeyNotFoundException("Workflow instance not found");

        if (instance.Status != WorkflowStatus.Draft)
            throw new InvalidOperationException("Cannot add steps to non-draft workflow");

        instance.AddStep(type, dayOffset, timeOfDay, generationStrategy);
        var newStep = instance.Steps.Last();

        // Validate Dependencies immediately
        if (type == WorkflowStepType.Email || type == WorkflowStepType.LinkedInMessage)
        {
             var prospect = await _prospectRepo.GetByIdAsync(instance.ProspectId, ct)
                 ?? throw new InvalidOperationException("Prospect not found");

             if (prospect.GetActiveContact() == null)
                 throw new InvalidOperationException("Cannot add communication step: Prospect has no active contact person.");

             if (generationStrategy == ContentGenerationStrategy.UseCollectedData && prospect.EntityIntelligenceId == null)
                 throw new InvalidOperationException("Cannot add 'Use Collected Data' step: Prospect has no Entity Intelligence.");
        }

        // Generate draft immediately for new step
        if (newStep.Type == WorkflowStepType.Email || newStep.Type == WorkflowStepType.LinkedInMessage)
        {
            await _draftGenerator.GenerateDraftForStepAsync(newStep, instance.ProspectId, userId, ct);
        }

        await _repo.AddStepAsync(newStep, ct);
        await _repo.UpdateInstanceAsync(instance, ct);
        return instance;
    }
}
