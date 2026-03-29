using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Application.DTOs.Workflows;

namespace Esatto.Outreach.Application.UseCases.Workflows;

public class UpdateWorkflowTemplate
{
    private readonly IWorkflowRepository _repo;
    public UpdateWorkflowTemplate(IWorkflowRepository repo) => _repo = repo;

    public async Task Handle(Guid id, string name, string? description, List<WorkflowTemplateStepInputDto> steps, CancellationToken ct = default)
    {
        var template = await _repo.GetTemplateByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"Template {id} not found");

        template.Update(name, description);
        template.ClearSteps();
        
        foreach (var s in steps)
        {
            template.AddStep(s.Type, s.DayOffset, s.TimeOfDay, s.GenerationStrategy);
        }
        
        await _repo.UpdateTemplateAsync(template, ct);
    }
}
