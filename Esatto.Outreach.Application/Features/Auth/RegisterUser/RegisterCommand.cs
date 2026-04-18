namespace Esatto.Outreach.Application.Features.Auth.RegisterUser;

public sealed record RegisterCommand(
    string Email,
    string Password,
    string? FullName,
    string CompanyName
);
