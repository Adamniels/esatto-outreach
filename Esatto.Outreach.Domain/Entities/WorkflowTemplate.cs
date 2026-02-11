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
        // Sort steps by DayOffset, then by TimeOfDay
        var ordered = Steps
            .OrderBy(s => s.DayOffset)
            .ThenBy(s => s.TimeOfDay)
            .ToList();
        
        // Reassign OrderIndex sequentially
        for (int i = 0; i < ordered.Count; i++)
        {
            ordered[i].SetOrderIndex(i);
        }
        
        Touch();
    }
}

public class WorkflowTemplateStep : Entity
{
    public Guid WorkflowTemplateId { get; private set; }

    public int OrderIndex { get; private set; }
    public int DayOffset { get; private set; }
    public TimeSpan TimeOfDay { get; private set; }

    public WorkflowStepType StepType { get; private set; }
    public ContentGenerationStrategy? GenerationStrategy { get; private set; }

    protected WorkflowTemplateStep() {}

    public static WorkflowTemplateStep Create(WorkflowStepType type, int dayOffset, TimeSpan timeOfDay, int orderIndex, ContentGenerationStrategy? generationStrategy = null)
    {
        // Validate: Email and LinkedIn Message steps MUST have a generation strategy
        if ((type == WorkflowStepType.Email || type == WorkflowStepType.LinkedInMessage) && generationStrategy == null)
        {
            throw new ArgumentException($"{type} step requires a content generation strategy");
        }
        
        // Validate: Other step types should NOT have a generation strategy
        if (type != WorkflowStepType.Email && type != WorkflowStepType.LinkedInMessage && generationStrategy != null)
        {
            throw new ArgumentException($"{type} step should not have a content generation strategy");
        }
        
       return new WorkflowTemplateStep
       {
           StepType = type,
           DayOffset = dayOffset,
           TimeOfDay = timeOfDay,
           OrderIndex = orderIndex,
           GenerationStrategy = generationStrategy
       };
    }
    
    public void SetOrderIndex(int newIndex)
    {
        OrderIndex = newIndex;
        Touch();
    }
    
    public void UpdateConfiguration(WorkflowStepType type, int dayOffset, TimeSpan timeOfDay, WorkflowTemplate template, ContentGenerationStrategy? generationStrategy = null)
    {
        // Validate: Email and LinkedIn Message steps MUST have a generation strategy
        if ((type == WorkflowStepType.Email || type == WorkflowStepType.LinkedInMessage) && generationStrategy == null)
        {
            throw new ArgumentException($"{type} step requires a content generation strategy");
        }
        
        // Validate: Other step types should NOT have a generation strategy
        if (type != WorkflowStepType.Email && type != WorkflowStepType.LinkedInMessage && generationStrategy != null)
        {
            throw new ArgumentException($"{type} step should not have a content generation strategy");
        }
        
        StepType = type;
        DayOffset = dayOffset;
        TimeOfDay = timeOfDay;
        GenerationStrategy = generationStrategy;
        Touch();
        
        // Trigger reordering of all steps after updating this step's day/time
        template?.ReorderSteps();
    }
}
