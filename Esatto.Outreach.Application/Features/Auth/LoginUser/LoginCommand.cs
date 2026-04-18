namespace Esatto.Outreach.Application.Features.Auth.LoginUser;

public sealed record LoginCommand(
    string Email,
    string Password
);
