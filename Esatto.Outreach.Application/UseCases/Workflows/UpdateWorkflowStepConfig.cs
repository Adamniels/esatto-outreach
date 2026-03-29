using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Domain.Enums;

namespace Esatto.Outreach.Application.UseCases.Workflows;

public class UpdateWorkflowStepConfig
{
    private readonly IWorkflowRepository _repo;
    public UpdateWorkflowStepConfig(IWorkflowRepository repo) => _repo = repo;

    public async Task Handle(Guid stepId, WorkflowStepType type, int dayOffset, TimeSpan timeOfDay, ContentGenerationStrategy? generationStrategy, CancellationToken ct = default)
    {
        var step = await _repo.GetStepByIdAsync(stepId, ct)
            ?? throw new KeyNotFoundException("Step not found");
        
        // Get the parent instance to ensure reordering changes are saved
        var instance = await _repo.GetInstanceByIdAsync(step.WorkflowInstanceId, ct)
            ?? throw new KeyNotFoundException("Workflow instance not found");
        
        step.UpdateConfiguration(type, dayOffset, timeOfDay, generationStrategy);
        
        // UpdateConfiguration calls ReorderSteps() which modifies OrderIndex on all steps
        // So we need to update the entire instance to persist all changes
        await _repo.UpdateInstanceAsync(instance, ct);
    }
}
