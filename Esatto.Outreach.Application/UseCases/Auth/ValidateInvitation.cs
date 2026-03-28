using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Application.DTOs.Auth;

namespace Esatto.Outreach.Application.UseCases.Auth;

/// <summary>
/// Validates an invitation token and returns company name and email if valid.
/// </summary>
public sealed class ValidateInvitation
{
    private readonly IInvitationRepository _invitationRepo;

    public ValidateInvitation(IInvitationRepository invitationRepo)
    {
        _invitationRepo = invitationRepo;
    }

    /// <summary>
    /// Returns validation result if token is valid and not expired/used; otherwise null.
    /// </summary>
    public async Task<ValidateInvitationDto?> Handle(string token, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(token))
            return null;

        var hashedToken = ComputeSha256(token.Trim());

        var invitation = await _invitationRepo.GetByTokenAsync(hashedToken, ct);
        if (invitation == null)
            return null;
        if (invitation.UsedAt != null)
            return null;
        if (invitation.ExpiresAt < DateTime.UtcNow)
            return null;

        return new ValidateInvitationDto(invitation.Company.Name, invitation.Email);
    }

    private static string ComputeSha256(string input)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(input);
        var hash = System.Security.Cryptography.SHA256.HashData(bytes);
        return Convert.ToBase64String(hash);
    }
}
