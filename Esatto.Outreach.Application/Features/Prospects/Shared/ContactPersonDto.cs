using Esatto.Outreach.Domain.Entities;

namespace Esatto.Outreach.Application.Features.Prospects.Shared;

public record ContactPersonDto(
    Guid Id,
    Guid ProspectId,
    string Name,
    string? Title,
    string? Email,
    string? LinkedInUrl,
    List<string>? PersonalHooks,
    List<string>? PersonalNews,
    string? GeneralInfo,
    bool IsActive
) {
    public static ContactPersonDto FromEntity(ContactPerson e)
    {
        return new ContactPersonDto(
            e.Id,
            e.ProspectId,
            e.Name,
            e.Title,
            e.Email,
            e.LinkedInUrl,
            e.PersonalHooks,
            e.PersonalNews,
            e.Summary,
            e.IsActive
        );
    }
}
