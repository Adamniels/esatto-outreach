namespace Esatto.Outreach.Api.Requests.Auth;

public sealed record AcceptInvitationRequest(
    string Token,
    string Email,
    string Password,
    string? FullName
);
