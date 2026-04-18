namespace Esatto.Outreach.Application.Features.Auth.AcceptInvitation;

public record AcceptInvitationRequest(string Token, string Email, string Password, string? FullName);
