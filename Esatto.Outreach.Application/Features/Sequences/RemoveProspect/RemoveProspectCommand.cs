namespace Esatto.Outreach.Application.Features.Sequences.RemoveProspect;

public sealed record RemoveProspectCommand(
    Guid SequenceId,
    Guid SequenceProspectId
);
