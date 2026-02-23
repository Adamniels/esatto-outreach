namespace Esatto.Outreach.Application.DTOs;

/// <summary>
/// Response from validating an invite token.
/// </summary>
public record ValidateInvitationDto(string CompanyName, string Email);

/// <summary>
/// Request to accept an invitation and create/link user.
/// </summary>
public record AcceptInvitationDto(string Token, string Email, string Password, string? FullName);

/// <summary>
/// Request to create an invitation (authenticated user's company).
/// </summary>
public record CreateInvitationDto(string Email);

/// <summary>
/// Response after creating an invitation.
/// </summary>
public record CreateInvitationResponseDto(string Token, string? InviteLink);
