using Esatto.Outreach.Domain.Entities;
using System.Text.Json;

namespace Esatto.Outreach.Application.DTOs;

public record ContactPersonDto(
    Guid Id,
    Guid ProspectId,
    string Name,
    string? Title,
    string? Email,
    string? LinkedInUrl,
    List<string>? PersonalHooks,
    List<string>? PersonalNews,
    string? GeneralInfo  // Mapped from Summary
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
            e.Summary
        );
    }
}
