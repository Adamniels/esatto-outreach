using Esatto.Outreach.Application.Abstractions.Repositories;

namespace Esatto.Outreach.Application.UseCases.Workflows;

public class UpdateWorkflowStepContent
{
    private readonly IWorkflowRepository _repo;
    public UpdateWorkflowStepContent(IWorkflowRepository repo) => _repo = repo;

    public async Task Handle(Guid stepId, string? subject, string? body, CancellationToken ct = default)
    {
        var step = await _repo.GetStepByIdAsync(stepId, ct)
            ?? throw new KeyNotFoundException("Step not found");

        step.UpdateDraft(subject, body);
        await _repo.UpdateStepAsync(step, ct);
    }
}
