using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Domain.Enums;

namespace Esatto.Outreach.Application.UseCases.Workflows;

public class DeleteWorkflowStep
{
    private readonly IWorkflowRepository _repo;
    public DeleteWorkflowStep(IWorkflowRepository repo) => _repo = repo;

    public async Task Handle(Guid instanceId, Guid stepId, CancellationToken ct = default)
    {
        var instance = await _repo.GetInstanceByIdAsync(instanceId, ct)
            ?? throw new KeyNotFoundException("Workflow instance not found");

        if (instance.Status != WorkflowStatus.Draft)
             throw new InvalidOperationException("Cannot delete steps from non-draft workflow");

        var stepToRemove = instance.Steps.FirstOrDefault(s => s.Id == stepId)
            ?? throw new KeyNotFoundException("Step not found in instance");

        // Use Domain Logic to remove and re-index
        instance.RemoveStep(stepId);
        
        await _repo.DeleteStepAsync(stepToRemove, ct);
        await _repo.UpdateInstanceAsync(instance, ct);
    }
}
