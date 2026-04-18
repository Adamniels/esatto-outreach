namespace Esatto.Outreach.Application.Features.Prospects.UpdateContactPerson;

public sealed record UpdateContactPersonCommand(
    Guid ProspectId,
    Guid ContactId,
    string? Name,
    string? Title,
    string? Email,
    string? LinkedInUrl,
    List<string>? PersonalHooks,
    List<string>? PersonalNews,
    string? GeneralInfo
);
