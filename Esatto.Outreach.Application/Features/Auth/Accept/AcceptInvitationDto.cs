namespace Esatto.Outreach.Application.Features.Auth;

public record AcceptInvitationDto(string Token, string Email, string Password, string? FullName);
