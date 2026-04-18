namespace Esatto.Outreach.Application.Features.Auth.InviteUser;

public sealed record InviteUserCommand(
    string Email,
    string? FrontendBaseUrl
);
