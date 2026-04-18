using System.Security.Cryptography;
using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Application.Features.Auth;
using Esatto.Outreach.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace Esatto.Outreach.Application.Features.Auth;

/// <summary>
/// Creates an invitation for the given email to join the current user's company.
/// </summary>
public sealed class CreateInvitationCommandHandler
{
    private readonly IInvitationRepository _invitationRepo;
    private readonly UserManager<ApplicationUser> _userManager;

    private const int ExpiryDays = 7;

    public CreateInvitationCommandHandler(
        IInvitationRepository invitationRepo,
        UserManager<ApplicationUser> userManager)
    {
        _invitationRepo = invitationRepo;
        _userManager = userManager;
    }

    public async Task<CreateInvitationResponseDto> Handle(
        string createdById,
        string email,
        string? frontendBaseUrl,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email is required");

        var creator = await _userManager.FindByIdAsync(createdById);
        if (creator == null)
            throw new InvalidOperationException("User not found");
        if (creator.CompanyId == null)
            throw new InvalidOperationException("You must belong to a company to invite others");

        // Make sure user with this email does not already exist
        var existingUser = await _userManager.FindByEmailAsync(email);
        if (existingUser != null)
            throw new InvalidOperationException("User with this email already exists");

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

        return new CreateInvitationResponseDto(rawToken, inviteLink);
    }

    private static string ComputeSha256(string input)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(input);
        var hash = SHA256.HashData(bytes);
        return Convert.ToBase64String(hash);
    }
}
