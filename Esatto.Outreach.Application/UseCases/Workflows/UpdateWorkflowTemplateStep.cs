using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Domain.Enums;

namespace Esatto.Outreach.Application.UseCases.Workflows;

public class UpdateWorkflowTemplateStep
{
    private readonly IWorkflowRepository _repo;
    public UpdateWorkflowTemplateStep(IWorkflowRepository repo) => _repo = repo;

    public async Task Handle(Guid stepId, WorkflowStepType type, int dayOffset, TimeSpan timeOfDay, ContentGenerationStrategy? generationStrategy, CancellationToken ct = default)
    {
        var templates = await _repo.GetAllTemplatesAsync(ct);
        Domain.Entities.WorkflowTemplate? template = null;
        Domain.Entities.WorkflowTemplateStep? step = null;
        
        // Find the template containing the step
        foreach (var t in templates)
        {
            step = t.Steps.FirstOrDefault(s => s.Id == stepId);
            if (step != null)
            {
                template = t;
                break;
            }
        }
        
        if (template == null || step == null)
            throw new InvalidOperationException($"Template step {stepId} not found");
        
        step.UpdateConfiguration(type, dayOffset, timeOfDay, template, generationStrategy);
        await _repo.UpdateTemplateAsync(template, ct);
    }
}
