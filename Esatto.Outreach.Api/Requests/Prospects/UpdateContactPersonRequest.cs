namespace Esatto.Outreach.Api.Requests.Prospects;

public sealed record UpdateContactPersonRequest(
    string? Name,
    string? Title,
    string? Email,
    string? LinkedInUrl,
    List<string>? PersonalHooks,
    List<string>? PersonalNews,
    string? GeneralInfo
);
