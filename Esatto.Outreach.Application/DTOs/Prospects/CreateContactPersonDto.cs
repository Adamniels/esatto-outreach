namespace Esatto.Outreach.Application.DTOs.Prospects;

public record CreateContactPersonDto(
    string Name,
    string? Title,
    string? Email,
    string? LinkedInUrl);
