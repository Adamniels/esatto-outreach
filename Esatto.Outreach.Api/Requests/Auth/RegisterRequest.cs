namespace Esatto.Outreach.Api.Requests.Auth;

public sealed record RegisterRequest(
    string Email,
    string Password,
    string? FullName,
    string CompanyName
);
