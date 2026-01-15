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
        List<string>? DeserializeList(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return new();
            try { return JsonSerializer.Deserialize<List<string>>(json) ?? new(); }
            catch { return new(); }
        }

        return new ContactPersonDto(
            e.Id,
            e.ProspectId,
            e.Name,
            e.Title,
            e.Email,
            e.LinkedInUrl,
            DeserializeList(e.PersonalHooksJson),
            DeserializeList(e.PersonalNewsJson),
            e.Summary
        );
    }
}
