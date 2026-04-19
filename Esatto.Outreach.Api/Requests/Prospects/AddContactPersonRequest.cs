namespace Esatto.Outreach.Api.Requests.Prospects;

public sealed record AddContactPersonRequest(
    string Name,
    string? Title,
    string? Email,
    string? LinkedInUrl
);
