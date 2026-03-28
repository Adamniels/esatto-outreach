using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Domain.Entities;

namespace Esatto.Outreach.Application.UseCases.Workflows;

public class ListWorkflowInstances
{
    private readonly IWorkflowRepository _repo;
    public ListWorkflowInstances(IWorkflowRepository repo) => _repo = repo;

    public async Task<List<WorkflowInstance>> Handle(Guid prospectId, CancellationToken ct = default)
    {
        return await _repo.GetInstancesByProspectIdAsync(prospectId, ct);
    }
}
