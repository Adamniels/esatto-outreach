using Esatto.Outreach.Application.Abstractions.Repositories;

namespace Esatto.Outreach.Application.UseCases.Workflows;

public class DeleteWorkflowTemplate
{
    private readonly IWorkflowRepository _repo;
    public DeleteWorkflowTemplate(IWorkflowRepository repo) => _repo = repo;

    public async Task Handle(Guid id, CancellationToken ct = default)
    {
        await _repo.DeleteTemplateAsync(id, ct);
    }
}
