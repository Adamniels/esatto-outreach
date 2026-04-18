using Esatto.Outreach.Domain.Common;
using Esatto.Outreach.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Esatto.Outreach.Domain.Entities.SequenceFeature;

public class SequenceProspect : Entity
{
    public Guid SequenceId { get; private set; }
    public Sequence Sequence { get; private set; } = default!;

    public Guid ProspectId { get; private set; }
    public Prospect Prospect { get; private set; } = default!;

    public Guid ContactPersonId { get; private set; }
    public ContactPerson Contact { get; private set; } = default!;

    public SequenceProspectStatus Status { get; private set; }

    public int CurrentStepIndex { get; private set; }
    public DateTime? NextStepScheduledAt { get; private set; }
    public DateTime? LastStepExecutedAt { get; private set; }
    public DateTime? ActivatedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    public string? FailureReason { get; private set; }

    [Timestamp]
    public byte[] RowVersion { get; private set; } = Guid.NewGuid().ToByteArray();

    protected SequenceProspect() { }

    public static SequenceProspect Create(Guid sequenceId, Guid prospectId, Guid contactPersonId)
    {
        return new SequenceProspect
        {
            SequenceId = sequenceId,
            ProspectId = prospectId,
            ContactPersonId = contactPersonId,
            Status = SequenceProspectStatus.Pending,
            CurrentStepIndex = 0
        };
    }

    public void Activate(DateTime nextStepScheduledAt)
    {
        if (Status != SequenceProspectStatus.Pending)
            throw new InvalidOperationException("Can only activate pending prospects");

        Status = SequenceProspectStatus.Active;
        ActivatedAt = DateTime.UtcNow;
        NextStepScheduledAt = nextStepScheduledAt;
        Touch();
    }

    public void MarkStepCompleted(DateTime nextStepScheduledAt)
    {
        if (Status != SequenceProspectStatus.Active)
            throw new InvalidOperationException("Can only complete step for active prospects");

        CurrentStepIndex++;
        LastStepExecutedAt = DateTime.UtcNow;
        NextStepScheduledAt = nextStepScheduledAt;
        Touch();
    }

    public void MarkSequenceCompleted()
    {
        if (Status != SequenceProspectStatus.Active)
            throw new InvalidOperationException("Can only complete sequence for active prospects");

        Status = SequenceProspectStatus.Completed;
        CompletedAt = DateTime.UtcNow;
        NextStepScheduledAt = null;
        LastStepExecutedAt = DateTime.UtcNow;
        CurrentStepIndex++; // Push it past the last index just to be clear it's done
        Touch();
    }

    public void MarkFailed(string reason)
    {
        Status = SequenceProspectStatus.Failed;
        FailureReason = reason;
        NextStepScheduledAt = null;
        Touch();
    }

    public void LeaseForExecution(DateTime leaseUntilUtc)
    {
        if (Status != SequenceProspectStatus.Active)
            throw new InvalidOperationException("Can only lease active prospects");

        NextStepScheduledAt = leaseUntilUtc;
        Touch();
    }

    /// <summary>
    /// If there is no step at the current execution index, marks this enrollment complete. Used by the worker before executing.
    /// </summary>
    /// <returns>true if the enrollment was completed and execution should be skipped.</returns>
    public bool TryCompleteIfNoCurrentStep(Sequence sequence)
    {
        if (sequence.GetStepAtExecutionIndex(CurrentStepIndex) != null)
            return false;

        MarkSequenceCompleted();
        return true;
    }

    /// <summary>After a step executor succeeds, advances schedule or completes the enrollment.</summary>
    public void RecordSuccessfulStepAndScheduleNext(Sequence sequence, DateTime utcNow)
    {
        var nextIndex = CurrentStepIndex + 1;
        var nextStep = sequence.GetStepAtExecutionIndex(nextIndex);
        if (nextStep == null)
        {
            MarkSequenceCompleted();
        }
        else
        {
            var scheduledDate = utcNow.Date.AddDays(nextStep.DelayInDays);
            if (nextStep.TimeOfDayToRun.HasValue)
            {
                var runAt = nextStep.TimeOfDayToRun.Value switch
                {
                    TimeOfDay.EarlyMorning => new TimeSpan(6, 0, 0),
                    TimeOfDay.LateMorning => new TimeSpan(10, 0, 0),
                    TimeOfDay.EarlyAfternoon => new TimeSpan(13, 0, 0),
                    TimeOfDay.LateAfternoon => new TimeSpan(16, 0, 0),
                    TimeOfDay.Evening => new TimeSpan(19, 0, 0),
                    _ => new TimeSpan(10, 0, 0)
                };
                scheduledDate = scheduledDate.Add(runAt);
            }
            else
            {
                scheduledDate = utcNow.AddDays(nextStep.DelayInDays);
            }

            MarkStepCompleted(scheduledDate);
        }
    }
}
