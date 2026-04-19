namespace Esatto.Outreach.Api.Requests.Auth;

public sealed record LoginRequest(
    string Email,
    string Password
);
