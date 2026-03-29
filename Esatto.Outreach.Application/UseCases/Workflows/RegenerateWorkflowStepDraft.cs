using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Application.Services;
using Esatto.Outreach.Domain.Enums;

namespace Esatto.Outreach.Application.UseCases.Workflows;

public class RegenerateWorkflowStepDraft
{
    private readonly IWorkflowRepository _repo;
    private readonly IProspectRepository _prospectRepo;
    private readonly WorkflowDraftGenerator _draftGenerator;

    public RegenerateWorkflowStepDraft(IWorkflowRepository repo, IProspectRepository prospectRepo, WorkflowDraftGenerator draftGenerator)
    {
        _repo = repo;
        _prospectRepo = prospectRepo;
        _draftGenerator = draftGenerator;
    }

    public async Task Handle(Guid stepId, string userId, CancellationToken ct = default)
    {
        var step = await _repo.GetStepByIdAsync(stepId, ct)
            ?? throw new KeyNotFoundException("Step not found");
        
        // Validate dependencies before regenerating
        if (step.GenerationStrategy == ContentGenerationStrategy.UseCollectedData)
        {
             var prospect = await _prospectRepo.GetByIdAsync(step.WorkflowInstance.ProspectId, ct);
             if (prospect?.EntityIntelligenceId == null)
                 throw new InvalidOperationException("Cannot regenerate draft: Missing Entity Intelligence for 'UseCollectedData' strategy.");
        }
        
        await _draftGenerator.GenerateDraftForStepAsync(step, step.WorkflowInstance.ProspectId, userId, ct);
        await _repo.UpdateStepAsync(step, ct);
    }
}
