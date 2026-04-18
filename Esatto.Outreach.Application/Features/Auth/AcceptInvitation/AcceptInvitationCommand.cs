namespace Esatto.Outreach.Application.Features.Auth.AcceptInvitation;

public sealed record AcceptInvitationCommand(
    string Token,
    string Email,
    string Password,
    string? FullName
);
