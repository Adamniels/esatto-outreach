namespace Esatto.Outreach.Domain.Enums;

public enum SequenceMode
{
    Focused,
    Multi
}

public enum SequenceStatus
{
    Setup,
    Draft,
    Active,
    Paused,
    Completed,
    Archived,
    Failed
}

public enum SequenceStepType
{
    Email,
    LinkedInMessage,
    LinkedInConnectionRequest,
    LinkedInInteraction
}

public enum TimeOfDay
{
    EarlyMorning, // 5am - 8am
    LateMorning, // 8am - 12pm
    EarlyAfternoon, // 12pm - 3pm
    LateAfternoon, // 3pm - 6pm
    Evening // 6pm - 9pm
}

public enum SequenceProspectStatus
{
    Pending,
    Active,
    Completed,
    Failed
}
