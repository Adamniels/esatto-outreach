namespace Esatto.Outreach.Application.Features.Prospects;

public record UpdateContactPersonDto(
    string Name,
    string? Title,
    string? Email,
    string? LinkedInUrl,
    List<string>? PersonalHooks,
    List<string>? PersonalNews,
    string? GeneralInfo
);
