using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Application.DTOs.Workflows;
using Esatto.Outreach.Domain.Entities;

namespace Esatto.Outreach.Application.UseCases.Workflows;

public class CreateWorkflowTemplate
{
    private readonly IWorkflowRepository _repo;
    public CreateWorkflowTemplate(IWorkflowRepository repo) => _repo = repo;

    public async Task<WorkflowTemplate> Handle(string name, string? description, List<WorkflowTemplateStepInputDto>? steps, CancellationToken ct = default)
    {
        var template = WorkflowTemplate.Create(name, description);
        
        if (steps != null)
        {
            foreach (var s in steps)
            {
                template.AddStep(s.Type, s.DayOffset, s.TimeOfDay, s.GenerationStrategy);
            }
        }

        await _repo.AddTemplateAsync(template, ct);
        return template;
    }
}
