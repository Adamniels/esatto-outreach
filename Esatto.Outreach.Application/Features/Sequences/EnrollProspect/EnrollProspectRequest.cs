namespace Esatto.Outreach.Application.Features.Sequences.EnrollProspect;

public record EnrollProspectRequest(
    Guid ProspectId,
    Guid ContactPersonId
);
