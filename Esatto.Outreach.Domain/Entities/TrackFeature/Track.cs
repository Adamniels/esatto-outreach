//TODO: clean this file up, dont want everything in this file, but for now its easier to keep it together

using Esatto.Outreach.Domain.Common;

namespace Esatto.Outreach.Domain.Entities;

public class Track : Entity
{
    public Guid WorkflowInstanceId { get; private set; }

    public WorkflowInstance? WorkflowInstance
    {
        get; private set;
    }

    public List<TrackProspect> TrackProspects { get; private set; } = new();

    // TODO: add settings entity later, not sure how I want it to look like yet

    public string? OwnerId { get; private set; }
    public ApplicationUser? Owner { get; private set; }
}


public class TrackProspect : Entity
{
    public Guid TrackId { get; private set; }
    public Track Track { get; private set; } = default!;

    public Guid ProspectId { get; private set; }
    public Prospect Prospect { get; private set; } = default!;

    // NOTE: in the future I think i want to have a list of contact persons,
    // so if we don't get an answer from one, we can try the next.
    public Guid ContactPersonId { get; private set; }
    public ContactPerson Contact { get; private set; } = default!;

    // Status to track each prospect's progress through the track, because i probably 
    // don't want to run them all, can lead to ban or flagging.
    public TrackProspectStatus Status { get; private set; }
}

public enum TrackProspectStatus
{
    Pending,
    Active,
    Completed,
    Failed
}
