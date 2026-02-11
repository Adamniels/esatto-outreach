using Esatto.Outreach.Application.Abstractions;
using Esatto.Outreach.Domain.Entities;
using Esatto.Outreach.Domain.Enums;

namespace Esatto.Outreach.Application.UseCases.Workflows;

// DTO used for step definition in create/update requests
public record WorkflowTemplateStepDTO(WorkflowStepType Type, int DayOffset, TimeSpan TimeOfDay, ContentGenerationStrategy? GenerationStrategy);

public class WorkflowTemplateService
{
    private readonly IWorkflowRepository _repo;

    public WorkflowTemplateService(IWorkflowRepository repo)
    {
        _repo = repo;
    }

    public async Task<WorkflowTemplate> CreateTemplateAsync(string name, string? description, List<WorkflowTemplateStepDTO>? steps, CancellationToken ct)
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

    public async Task<WorkflowTemplate?> GetTemplateAsync(Guid id, CancellationToken ct)
    {
        return await _repo.GetTemplateByIdAsync(id, ct);
    }

    public async Task<List<WorkflowTemplate>> GetAllTemplatesAsync(CancellationToken ct)
    {
        return await _repo.GetAllTemplatesAsync(ct);
    }

    public async Task UpdateTemplateAsync(Guid id, string name, string? description, List<WorkflowTemplateStepDTO> steps, CancellationToken ct)
    {
        var template = await _repo.GetTemplateByIdAsync(id, ct);
        if (template == null) throw new KeyNotFoundException($"Template {id} not found");

        template.Update(name, description);
        template.ClearSteps();
        
        foreach (var s in steps)
        {
            template.AddStep(s.Type, s.DayOffset, s.TimeOfDay, s.GenerationStrategy);
        }
        
        await _repo.UpdateTemplateAsync(template, ct);
    }

    public async Task DeleteTemplateAsync(Guid id, CancellationToken ct)
    {
        await _repo.DeleteTemplateAsync(id, ct);
    }

    public async Task SetDefaultAsync(Guid id, CancellationToken ct)
    {
        var templates = await _repo.GetAllTemplatesAsync(ct);
        foreach (var t in templates)
        {
            if (t.Id == id) t.SetDefault(true);
            else if (t.IsDefault) t.SetDefault(false);
            await _repo.UpdateTemplateAsync(t, ct);
        }
    }
    
    public async Task UpdateTemplateStepAsync(Guid stepId, WorkflowStepType type, int dayOffset, TimeSpan timeOfDay, ContentGenerationStrategy? generationStrategy, CancellationToken ct)
    {
        var templates = await _repo.GetAllTemplatesAsync(ct);
        WorkflowTemplate? template = null;
        WorkflowTemplateStep? step = null;
        
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
