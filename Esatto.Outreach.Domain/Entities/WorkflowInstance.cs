using Esatto.Outreach.Domain.Common;
using Esatto.Outreach.Domain.Enums;
using Esatto.Outreach.Domain.Extensions;

namespace Esatto.Outreach.Domain.Entities;

public class WorkflowInstance : Entity
{
    public Guid ProspectId { get; private set; }
    public Prospect Prospect { get; private set; } = default!;

    public WorkflowStatus Status { get; private set; } = WorkflowStatus.Draft;
    
    public DateTime CreatedAt { get; private set; }
    public DateTime? StartedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    public List<WorkflowStep> Steps { get; private set; } = new();

    public string TimeZoneId { get; private set; } = "UTC";

    // Optimistic Concurrency Token is inherited from Entity (if it has one?)
    // Checking Common/Entity.cs (assumed) usually has Id.
    // If we need RowVersion, we'll add it in DB Context config or here.

    protected WorkflowInstance() { }

    public static WorkflowInstance Create(Guid prospectId)
    {
        return new WorkflowInstance
        {
            ProspectId = prospectId,
            CreatedAt = DateTime.UtcNow,
            Status = WorkflowStatus.Draft,
            TimeZoneId = "UTC"
        };
    }

    public void AddStep(WorkflowStepType type, int dayOffset, TimeSpan timeOfDay, ContentGenerationStrategy? generationStrategy = null)
    {
        var step = WorkflowStep.Create(type, dayOffset, timeOfDay, Steps.Count, generationStrategy);
        Steps.Add(step);
        Touch();
    }

    public void AddStep(WorkflowStep step)
    {
        Steps.Add(step);
        Touch();
    }

    public void Activate(DateTime startedAt, string timeZoneId)
    {
        if (Status != WorkflowStatus.Draft)
            throw new InvalidOperationException($"Cannot activate workflow in state {Status}");

        // Validate TimeZone
        try 
        { 
            TimeZoneInfo.FindSystemTimeZoneById(timeZoneId); 
        }
        catch (Exception ex) 
        { 
            throw new ArgumentException($"Invalid TimeZoneId: {timeZoneId}. {ex.Message}", nameof(timeZoneId), ex); 
        }

        Status = WorkflowStatus.Active;
        StartedAt = startedAt;
        TimeZoneId = timeZoneId;
        
        // Recalculate run times for all steps based on StartAt
        foreach (var step in Steps)
        {
            step.Schedule(startedAt, timeZoneId);
        }
        
        Touch();
    }

    public void Cancel()
    {
        Status = WorkflowStatus.Cancelled;
        Touch();
    }
    
    public void Complete()
    {
        Status = WorkflowStatus.Completed;
        CompletedAt = DateTime.UtcNow;
        Touch();
    }
    public void RemoveStep(Guid stepId)
    {
        var step = Steps.FirstOrDefault(s => s.Id == stepId);
        if (step == null) return;
        
        Steps.Remove(step);
        
        // Re-index remaining steps
        var ordered = Steps.OrderBy(s => s.OrderIndex).ToList();
        for (int i = 0; i < ordered.Count; i++)
        {
            // We can't set OrderIndex directly if it's private set?
            // OrderIndex has private set. We need a method or internal access.
            // Since Step is nested or same assembly, internal works?
            // Actually it is same file, so private set is accessible if nested class? No.
            // They are separate classes in same namespace.
            // Let's assume we add a method to Step to set OrderIndex for re-indexing.
            ordered[i].SetOrderIndex(i);
        }
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
    
    /// <summary>
    /// Validates whether this workflow can be activated.
    /// Returns a list of error messages if validation fails, or an empty list if it can be activated.
    /// </summary>
    public List<string> CanActivate(bool hasEntityIntelligence)
    {
        var errors = new List<string>();
        
        foreach (var step in Steps)
        {
            if (step.Type.RequiresContent() && step.GenerationStrategy == null)
            {
                errors.Add($"Step {step.OrderIndex + 1} ({step.Type}) is missing a generation strategy");
            }
            
            if (step.GenerationStrategy == ContentGenerationStrategy.UseCollectedData 
                && !hasEntityIntelligence)
            {
                errors.Add($"Step {step.OrderIndex + 1} ({step.Type}) requires Entity Intelligence but prospect is not enriched. Please enrich the prospect first.");
            }
        }
        
        return errors;
    }
}
