//TODO: clean this file up, dont want everything in this file, but for now its easier to keep it together

using Esatto.Outreach.Domain.Common;

namespace Esatto.Outreach.Domain.Entities;

public class Sequence : Entity
{
    public string Title { get; private set; } = default!;
    public string? Description { get; private set; }

    public SequenceMode Mode { get; private set; }

    public SequenceStatus Status { get; private set; }

    public string? OwnerId { get; private set; }
    public ApplicationUser? Owner { get; private set; }

    public List<SequenceStep> SequenceSteps { get; private set; } = new();
    public List<SequenceProspect> SequenceProspects { get; private set; } = new();

    public SequenceSettings Settings { get; private set; } = default!;
    public string? MultiEnrichment{ get; private set; } // for multi mode

}

public class SequenceSettings
{
    public bool? EnrichCompany { get; private set; } // only available for focuses mode
    public bool? EnrichContact { get; private set; } // only available for focuses mode
    public bool? ResearchSimilarities { get; private set; } // only available for Multi mode

}
public enum SequenceMode
{
    Focused,
    Multi
}
public enum SequenceStatus
{
    Active,
    Paused,
    Completed,
    Archived,
    Failed
}

public class SequenceStep : Entity
{
    public Guid SequenceId { get; private set; }
    public Sequence Sequence { get; private set; } = default!;

    public int OrderIndex { get; private set; }
    public SequenceStepType StepType { get; private set; }

    public int DelayInDays { get; private set; } // delay after previous step, for now just in days, but might want to make it more flexible in the future.
    public TimeOfDay? TimeOfDayToRun { get; private set; } // TimeSpan, preferred send time

    public string? GeneratedSubject { get; private set; } // for email, generated subject line based on template and prospect data
    public string? GeneratedBody { get; private set; } // for email and LinkedIn

    public bool? UseCollectedData { get; private set; } // for all generation steps
}

public enum TimeOfDay
{
    EarlyMorning, // 5am - 8am
    LateMorning, // 8am - 12pm
    EarlyAfternoon, // 12pm - 3pm
    LateAfternoon, // 3pm - 6pm
    Evening // 6pm - 9pm
}


public enum SequenceStepType
{
    Email,
    LinkedInConnectionRequest,
    LinkedInInteraction,
    LinkedInMessage
}


public class SequenceProspect : Entity
{
    public Guid SequenceId { get; private set; }
    public Sequence Sequence { get; private set; } = default!;

    public Guid ProspectId { get; private set; }
    public Prospect Prospect { get; private set; } = default!;

    // NOTE: in the future I think i want to have a list of contact persons,
    // so if we don't get an answer from one, we can try the next.
    public Guid ContactPersonId { get; private set; }
    public ContactPerson Contact { get; private set; } = default!;

    // Status to track each prospect's progress through the sequence, because i probably 
    // don't want to run them all, can lead to ban or flagging.
    public SequenceProspectStatus Status { get; private set; }

    public int CurrentStepIndex { get; private set; } // to track which step they are on in the sequence
    public DateTime? NextStepScheduledAt { get; private set; } // to track when the next step should be executed for this prospect
    public DateTime? LastStepExecutedAt { get; private set; } // to track when the last step was executed, useful for analytics and debugging
    public DateTime? ActivatedAt { get; private set; } // to track when the sequence was activated for this prospect
    public DateTime? CompletedAt { get; private set; } // to track when the sequence was completed for this prospect

    public string? FailureReason { get; private set; } // to track why a sequence failed for this prospect, useful for analytics and debugging

    public byte[] RowVersion { get; private set; } = default!; // for concurrency control
}

public enum SequenceProspectStatus
{
    Pending,
    Active,
    Completed,
    Failed
}



