namespace Esatto.Outreach.Application.Features.Prospects.AddContactPerson;

public sealed record AddContactPersonCommand(
    Guid ProspectId,
    string Name,
    string? Title,
    string? Email,
    string? LinkedInUrl
);
