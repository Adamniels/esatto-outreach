using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Domain.Enums;

namespace Esatto.Outreach.Application.UseCases.Workflows;

public class ActivateWorkflowInstance
{
    private readonly IWorkflowRepository _repo;
    public ActivateWorkflowInstance(IWorkflowRepository repo) => _repo = repo;

    public async Task Handle(Guid instanceId, string timeZoneId, CancellationToken ct = default)
    {
        var instance = await _repo.GetInstanceByIdAsync(instanceId, ct)
            ?? throw new KeyNotFoundException("Workflow instance not found");

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
}
