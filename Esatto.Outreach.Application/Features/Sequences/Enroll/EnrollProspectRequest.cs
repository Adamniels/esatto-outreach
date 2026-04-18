namespace Esatto.Outreach.Application.Features.Sequences;

public record EnrollProspectRequest(
    Guid ProspectId,
    Guid ContactPersonId
);
