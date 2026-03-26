namespace Esatto.Outreach.Application.DTOs.Prospects;

public record UpdateContactPersonDto(
    string Name,
    string? Title,
    string? Email,
    string? LinkedInUrl,
    List<string>? PersonalHooks,
    List<string>? PersonalNews,
    string? GeneralInfo
);
