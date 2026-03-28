using Esatto.Outreach.Application.Abstractions.Repositories;

namespace Esatto.Outreach.Application.UseCases.Workflows;

public class SetDefaultWorkflowTemplate
{
    private readonly IWorkflowRepository _repo;
    public SetDefaultWorkflowTemplate(IWorkflowRepository repo) => _repo = repo;

    public async Task Handle(Guid id, CancellationToken ct = default)
    {
        var templates = await _repo.GetAllTemplatesAsync(ct);
        foreach (var t in templates)
        {
            if (t.Id == id) t.SetDefault(true);
            else if (t.IsDefault) t.SetDefault(false);
            await _repo.UpdateTemplateAsync(t, ct);
        }
    }
}
