namespace Esatto.Outreach.Application.DTOs;

public record CreateContactPersonDto(
    string Name,
    string? Title,
    string? Email,
    string? LinkedInUrl);
