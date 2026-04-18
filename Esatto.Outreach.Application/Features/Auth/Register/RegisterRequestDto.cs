namespace Esatto.Outreach.Application.Features.Auth;

public record RegisterRequestDto(
    string Email,
    string Password,
    string? FullName,
    string CompanyName
);
