using Esatto.Outreach.Application.Abstractions.Repositories;

namespace Esatto.Outreach.Application.UseCases.Workflows;

public class DeleteWorkflowInstance
{
    private readonly IWorkflowRepository _repo;
    public DeleteWorkflowInstance(IWorkflowRepository repo) => _repo = repo;

    public async Task Handle(Guid prospectId, CancellationToken ct = default)
    {
        var instances = await _repo.GetInstancesByProspectIdAsync(prospectId, ct);
        foreach (var instance in instances)
        {
            await _repo.DeleteInstanceAsync(instance, ct);
        }
    }
}
