using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Domain.Entities;

namespace Esatto.Outreach.Application.UseCases.Workflows;

public class ListWorkflowTemplates
{
    private readonly IWorkflowRepository _repo;
    public ListWorkflowTemplates(IWorkflowRepository repo) => _repo = repo;

    public async Task<List<WorkflowTemplate>> Handle(CancellationToken ct = default)
    {
        return await _repo.GetAllTemplatesAsync(ct);
    }
}
