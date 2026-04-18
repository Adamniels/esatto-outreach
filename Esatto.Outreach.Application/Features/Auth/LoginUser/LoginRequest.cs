namespace Esatto.Outreach.Application.Features.Auth.LoginUser;

public record LoginRequest(
    string Email,
    string Password
);
