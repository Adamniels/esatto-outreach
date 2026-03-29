using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Domain.Entities;

namespace Esatto.Outreach.Application.UseCases.Workflows;

public class GetWorkflowTemplate
{
    private readonly IWorkflowRepository _repo;
    public GetWorkflowTemplate(IWorkflowRepository repo) => _repo = repo;

    public async Task<WorkflowTemplate?> Handle(Guid id, CancellationToken ct = default)
    {
        return await _repo.GetTemplateByIdAsync(id, ct);
    }
}
