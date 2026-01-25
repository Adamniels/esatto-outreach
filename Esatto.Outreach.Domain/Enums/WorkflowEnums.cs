namespace Esatto.Outreach.Domain.Enums;

public enum WorkflowStatus
{
    Draft = 0,
    Active = 1,
    Completed = 2,
    Cancelled = 3
}

public enum WorkflowStepStatus
{
    Pending = 0,
    Executing = 1,
    Succeeded = 2,
    Failed = 3,
    Skipped = 4
}

public enum WorkflowStepType
{
    Email = 0,
    LinkedInMessage = 1,
    LinkedInConnectionRequest = 2,
    LinkedInInteract = 3 // Like, Follow, etc.
}
