using System.Security.Cryptography;
using Esatto.Outreach.Application.Abstractions;
using Esatto.Outreach.Application.DTOs;
using Esatto.Outreach.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace Esatto.Outreach.Application.UseCases.Auth;

/// <summary>
/// Creates an invitation for the given email to join the current user's company.
/// </summary>
public sealed class CreateInvitation
{
    private readonly IInvitationRepository _invitationRepo;
    private readonly UserManager<ApplicationUser> _userManager;

    private const int ExpiryDays = 7;

    public CreateInvitation(
        IInvitationRepository invitationRepo,
        UserManager<ApplicationUser> userManager)
    {
        _invitationRepo = invitationRepo;
        _userManager = userManager;
    }

    public async Task<(bool Success, CreateInvitationResponseDto? Data, string? Error)> Handle(
        string createdById,
        string email,
        string? frontendBaseUrl,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(email))
            return (false, null, "Email is required");

        var creator = await _userManager.FindByIdAsync(createdById);
        if (creator == null)
            return (false, null, "User not found");
        if (creator.CompanyId == null)
            return (false, null, "You must belong to a company to invite others");

        // Make sure user with this email does not already exist
        var existingUser = await _userManager.FindByEmailAsync(email);
        if (existingUser != null)
            return (false, null, "User with this email already exists");

        var tokenBytes = RandomNumberGenerator.GetBytes(32);
        var rawToken = Microsoft.AspNetCore.WebUtilities.Base64UrlTextEncoder.Encode(tokenBytes);
        var hashedToken = ComputeSha256(rawToken);

        var invitation = new Invitation
        {
            CompanyId = creator.CompanyId.Value,
            Email = email.Trim(),
            TokenHash = hashedToken,
            CreatedById = createdById,
            ExpiresAt = DateTime.UtcNow.AddDays(ExpiryDays),
        };

        await _invitationRepo.AddAsync(invitation, ct);

        string? inviteLink = null;
        if (!string.IsNullOrWhiteSpace(frontendBaseUrl))
        {
            var baseUrl = frontendBaseUrl.TrimEnd('/');
            inviteLink = $"{baseUrl}/accept-invite?token={rawToken}";
        }

        return (true, new CreateInvitationResponseDto(rawToken, inviteLink), null);
    }

    private static string ComputeSha256(string input)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(input);
        var hash = SHA256.HashData(bytes);
        return Convert.ToBase64String(hash);
    }
}
