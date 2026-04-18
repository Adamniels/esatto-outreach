namespace Esatto.Outreach.Application.Features.Prospects.AddContactPerson;

public record AddContactPersonRequest(
    string Name,
    string? Title,
    string? Email,
    string? LinkedInUrl);
