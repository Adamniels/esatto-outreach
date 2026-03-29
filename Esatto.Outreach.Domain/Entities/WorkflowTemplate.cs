using Esatto.Outreach.Domain.Common;
using Esatto.Outreach.Domain.Enums;

namespace Esatto.Outreach.Domain.Entities;

public class WorkflowTemplate : Entity
{
    public string Name { get; private set; } = default!;
    public string? Description { get; private set; }
    public bool IsDefault { get; private set; }

    public List<WorkflowTemplateStep> Steps { get; private set; } = new();

    protected WorkflowTemplate() { }

    public static WorkflowTemplate Create(string name, string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required", nameof(name));

        return new WorkflowTemplate
        {
            Name = name.Trim(),
            Description = description,
            IsDefault = false
        };
    }

    public void Update(string name, string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required", nameof(name));

        Name = name.Trim();
        Description = description;
        Touch();
    }

    public void SetDefault(bool isDefault)
    {
        IsDefault = isDefault;
        Touch();
    }

    public void AddStep(WorkflowStepType type, int dayOffset, TimeSpan timeOfDay, ContentGenerationStrategy? generationStrategy = null)
    {
        var step = WorkflowTemplateStep.Create(type, dayOffset, timeOfDay, Steps.Count, generationStrategy);
        Steps.Add(step);
        Touch();
    }

    public void ClearSteps()
    {
        Steps.Clear();
        Touch();
    }
    
    public void ReorderSteps()
    {
        var ordered = Steps
            .OrderBy(s => s.DayOffset)
            .ThenBy(s => s.TimeOfDay)
            .ToList();
        
        for (int i = 0; i < ordered.Count; i++)
        {
            ordered[i].SetOrderIndex(i);
        }
        
        Touch();
    }
}
