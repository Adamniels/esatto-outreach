namespace Esatto.Outreach.Application.Features.Prospects.UpdateContactPerson;

public record UpdateContactPersonRequest(
    string? Name,
    string? Title,
    string? Email,
    string? LinkedInUrl,
    List<string>? PersonalHooks,
    List<string>? PersonalNews,
    string? GeneralInfo);
