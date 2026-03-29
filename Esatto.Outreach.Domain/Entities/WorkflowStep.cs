using Esatto.Outreach.Domain.Common;
using Esatto.Outreach.Domain.Enums;
using Esatto.Outreach.Domain.Extensions;

namespace Esatto.Outreach.Domain.Entities;

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

    public string? EmailSubject { get; private set; }
    public string? BodyContent { get; private set; }
    
    public string? FailureReason { get; private set; }
    public int RetryCount { get; private set; } = 0;

    // Concurrency token - managed by EF Core ValueGeneratedOnAddOrUpdate
    public byte[]? RowVersion { get; private set; } = null;

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
        type.ValidateGenerationStrategy(generationStrategy);
        
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
        Touch();
    }

    public void UpdateConfiguration(WorkflowStepType type, int dayOffset, TimeSpan timeOfDay, ContentGenerationStrategy? generationStrategy = null)
    {
        if (Status == WorkflowStepStatus.Executing || Status == WorkflowStepStatus.Succeeded)
             throw new InvalidOperationException("Cannot update configuration of executing/succeeded step");

        type.ValidateGenerationStrategy(generationStrategy);

        Type = type;
        DayOffset = dayOffset;
        TimeOfDay = timeOfDay;
        GenerationStrategy = generationStrategy;
        Touch();
        
        WorkflowInstance?.ReorderSteps();
    }

    public void Schedule(DateTime workflowStartedAt, string timeZoneId)
    {
        var tz = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        var localStarted = TimeZoneInfo.ConvertTimeFromUtc(workflowStartedAt, tz);
        
        var localStartDay00 = localStarted.Date;
        var targetLocal = localStartDay00.AddDays(DayOffset).Add(TimeOfDay);
        
        if (tz.IsInvalidTime(targetLocal))
        {
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
