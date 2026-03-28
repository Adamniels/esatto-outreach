using Esatto.Outreach.Application.Abstractions.Repositories;

namespace Esatto.Outreach.Application.UseCases.Workflows;

public class ValidateWorkflowActivation
{
    private readonly IWorkflowRepository _repo;
    public ValidateWorkflowActivation(IWorkflowRepository repo) => _repo = repo;

    public async Task<List<string>> Handle(Guid instanceId, CancellationToken ct = default)
    {
        var instance = await _repo.GetInstanceByIdAsync(instanceId, ct)
            ?? throw new KeyNotFoundException("Workflow instance not found");
        
        // Check if prospect has Entity Intelligence
        bool hasEntityIntelligence = instance.Prospect?.EntityIntelligence != null;
        
        // Use domain validation method
        return instance.CanActivate(hasEntityIntelligence);
    }
}
