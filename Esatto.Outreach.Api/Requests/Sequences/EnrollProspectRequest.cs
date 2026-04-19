namespace Esatto.Outreach.Api.Requests.Sequences;

public sealed record EnrollProspectRequest(
    Guid ProspectId,
    Guid ContactPersonId
);
