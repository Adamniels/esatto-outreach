namespace Esatto.Outreach.Application.Features.Sequences.EnrollProspect;

public sealed record EnrollProspectCommand(
    Guid SequenceId,
    Guid ProspectId,
    Guid ContactPersonId
);
