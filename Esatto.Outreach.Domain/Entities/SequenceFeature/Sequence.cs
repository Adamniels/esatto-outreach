//TODO: clean this file up, dont want everything in this file, but for now its easier to keep it together

using Esatto.Outreach.Domain.Common;

namespace Esatto.Outreach.Domain.Entities;

public class Sequence : Entity
{
    public List<SequenceStep> SequenceSteps { get; private set; } = new();
    public List<SequenceProspect> SequenceProspects { get; private set; } = new();

    // TODO: add settings entity later, not sure how I want it to look like yet

    public string? OwnerId { get; private set; }
    public ApplicationUser? Owner { get; private set; }
}

// TODO: want to have a few different types of steps, so for example:
// - LinkedIn follow
// - Email
// - LinkedIn message
// - Follow up email
// - Follow up LinkedIn message
// - Follow up email
// how can I handle this?
public class SequenceStep : Entity
{}

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
}

public enum SequenceProspectStatus
{
    Pending,
    Active,
    Completed,
    Failed
}
