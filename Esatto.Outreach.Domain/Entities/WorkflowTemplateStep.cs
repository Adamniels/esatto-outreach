using Esatto.Outreach.Domain.Common;
using Esatto.Outreach.Domain.Enums;
using Esatto.Outreach.Domain.Extensions;

namespace Esatto.Outreach.Domain.Entities;

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
        type.ValidateGenerationStrategy(generationStrategy);
        
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
        type.ValidateGenerationStrategy(generationStrategy);
        
        StepType = type;
        DayOffset = dayOffset;
        TimeOfDay = timeOfDay;
        GenerationStrategy = generationStrategy;
        Touch();
        
        template?.ReorderSteps();
    }
}
