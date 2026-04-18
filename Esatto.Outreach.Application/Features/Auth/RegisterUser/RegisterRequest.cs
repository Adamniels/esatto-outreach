namespace Esatto.Outreach.Application.Features.Auth.RegisterUser;

public record RegisterRequest(
    string Email,
    string Password,
    string? FullName,
    string CompanyName
);
