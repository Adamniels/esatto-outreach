namespace Esatto.Outreach.Application.Features.Prospects;

public record CreateContactPersonDto(
    string Name,
    string? Title,
    string? Email,
    string? LinkedInUrl);
