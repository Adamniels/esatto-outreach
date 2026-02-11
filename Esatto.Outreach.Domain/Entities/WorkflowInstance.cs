using Esatto.Outreach.Domain.Common;
using Esatto.Outreach.Domain.Enums;

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
            // Check 1: Email and LinkedIn Message steps must have a generation strategy
            if ((step.Type == WorkflowStepType.Email || step.Type == WorkflowStepType.LinkedInMessage) 
                && step.GenerationStrategy == null)
            {
                errors.Add($"Step {step.OrderIndex + 1} ({step.Type}) is missing a generation strategy");
            }
            
            // Check 2: If step uses UseCollectedData strategy, prospect must have Entity Intelligence
            if (step.GenerationStrategy == ContentGenerationStrategy.UseCollectedData 
                && !hasEntityIntelligence)
            {
                errors.Add($"Step {step.OrderIndex + 1} ({step.Type}) requires Entity Intelligence but prospect is not enriched. Please enrich the prospect first.");
            }
        }
        
        return errors;
    }
}

public class WorkflowStep : Entity
{
    public Guid WorkflowInstanceId { get; private set; }
    public WorkflowInstance WorkflowInstance { get; private set; } = default!;
    
    public WorkflowStepType Type { get; private set; }
    public int OrderIndex { get; private set; }
    public int DayOffset { get; private set; }
    public TimeSpan TimeOfDay { get; private set; }
    public ContentGenerationStrategy? GenerationStrategy { get; private set; }
    
    public DateTime? RunAt { get; private set; }
    public WorkflowStepStatus Status { get; private set; } = WorkflowStepStatus.Pending;

    // Content / Drafts
    public string? EmailSubject { get; private set; }
    public string? BodyContent { get; private set; } // Email Body or LinkedIn Message
    
    public string? FailureReason { get; private set; }
    public int RetryCount { get; private set; } = 0;

    // Concurrency Token - Database generates this automatically via trigger
    // DO NOT manually set this value - it's updated by database trigger on every save
    public byte[]? RowVersion { get; private set; } = null;

    // Note: Removed custom Touch() override
    // Base.Touch() from Entity class handles UpdatedUtc
    // RowVersion is automatically updated by EF Core ValueGeneratedOnAddOrUpdate

    // Concurrency Token for "Claiming"
    // We will use a dedicated RowVersion or simple byte array in EF.
    // Ideally we add: public byte[] RowVersion { get; set; } but usually this is shadow property.
    // Let's rely on EF Core shadow property for RowVersion, 
    // BUT since we need to explicitly query it, maybe explicit is better?
    // Let's stick to standard EF patterns configurd in DbContext.

    protected WorkflowStep() { }

    public static WorkflowStep Create(
        WorkflowStepType type, 
        int dayOffset,
        TimeSpan timeOfDay,
        int orderIndex,
        ContentGenerationStrategy? generationStrategy = null,
        string? emailSubject = null,
        string? bodyContent = null)
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
        
        return new WorkflowStep
        {
            Type = type,
            DayOffset = dayOffset,
            TimeOfDay = timeOfDay,
            OrderIndex = orderIndex,
            GenerationStrategy = generationStrategy,
            Status = WorkflowStepStatus.Pending,
            EmailSubject = emailSubject,
            BodyContent = bodyContent
        };
    }

    public void SetOrderIndex(int newIndex)
    {
        OrderIndex = newIndex;
        Touch(); // Assuming Touch is available
    }

    public void UpdateConfiguration(WorkflowStepType type, int dayOffset, TimeSpan timeOfDay, ContentGenerationStrategy? generationStrategy = null)
    {
        if (Status == WorkflowStepStatus.Executing || Status == WorkflowStepStatus.Succeeded)
             throw new InvalidOperationException("Cannot update configuration of executing/succeeded step");

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

        Type = type;
        DayOffset = dayOffset;
        TimeOfDay = timeOfDay;
        GenerationStrategy = generationStrategy;
        Touch();
        
        // Trigger reordering of all steps after updating this step's day/time
        WorkflowInstance?.ReorderSteps();
    }

    public void Schedule(DateTime workflowStartedAt, string timeZoneId)
    {
        // 1. Get TimeZone
        var tz = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        
        // 2. Convert StartedAt (UTC) to Local Time
        var localStarted = TimeZoneInfo.ConvertTimeFromUtc(workflowStartedAt, tz);
        
        // 3. Calculate Target Run Time in Local
        // "DayOffset" is relative to Start Day. Start Day starts at 00:00 Local.
        var localStartDay00 = localStarted.Date;
        var targetLocal = localStartDay00.AddDays(DayOffset).Add(TimeOfDay);
        
        // 4. Convert back to UTC
        // Handle invalid time (e.g. springing forward GAP)
        if (tz.IsInvalidTime(targetLocal))
        {
            // If time is invalid (skipped), add an hour? Or usually standard .NET behavior jumps forward.
            // Let's rely on standard mapping or assume valid day/time logic.
            // Actually .NET throws ArgumentException on invalid time for mapping back unless we assume UTC offset.
            // Safe approach: Add small delta until valid? Or just use map.
             // Simple hack: if invalid, add 1 hour.
             targetLocal = targetLocal.AddHours(1);
        }

        var utcRunAt = TimeZoneInfo.ConvertTimeToUtc(targetLocal, tz);
        
        RunAt = utcRunAt;
        Status = WorkflowStepStatus.Pending;
        Touch();
    }

    public void UpdateDraft(string? subject, string? body)
    {
        if (Status == WorkflowStepStatus.Executing || Status == WorkflowStepStatus.Succeeded)
             throw new InvalidOperationException("Cannot update draft of executing/succeeded step");

        EmailSubject = subject;
        BodyContent = body;
        Touch();
    }
    
    public void MarkExecuting()
    {
        // State transition validation
        if (Status != WorkflowStepStatus.Pending)
             throw new InvalidOperationException($"Cannot start executing step in state {Status}");
             
        Status = WorkflowStepStatus.Executing;
        Touch();
    }

    public void MarkSucceeded()
    {
        Status = WorkflowStepStatus.Succeeded;
        Touch();
    }

    public void MarkFailed(string reason)
    {
        Status = WorkflowStepStatus.Failed;
        FailureReason = reason;
        Touch();
    }
    
    public void Reset()
    {
        Status = WorkflowStepStatus.Pending;
        FailureReason = null;
        Touch();
    }
}
