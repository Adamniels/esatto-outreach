using System.Security.Cryptography;
using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;

namespace Esatto.Outreach.Application.Features.Auth.InviteUser;

public sealed class InviteUserCommandHandler
{
    private readonly IInvitationRepository _invitationRepo;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IUnitOfWork _unitOfWork;
    private readonly string? _frontendBaseUrl;

    private const int ExpiryDays = 7;

    public InviteUserCommandHandler(
        IInvitationRepository invitationRepo,
        UserManager<ApplicationUser> userManager,
        IUnitOfWork unitOfWork,
        IConfiguration configuration)
    {
        _invitationRepo = invitationRepo;
        _userManager = userManager;
        _unitOfWork = unitOfWork;
        _frontendBaseUrl = configuration["Frontend:BaseUrl"];
    }

    public async Task<InviteUserResponse> Handle(
        InviteUserCommand command,
        string createdById,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(command.Email))
            throw new ArgumentException("Email is required");

        var creator = await _userManager.FindByIdAsync(createdById);
        if (creator == null)
            throw new InvalidOperationException("User not found");
        if (creator.CompanyId == null)
            throw new InvalidOperationException("You must belong to a company to invite others");

        var existingUser = await _userManager.FindByEmailAsync(command.Email);
        if (existingUser != null)
            throw new InvalidOperationException("User with this email already exists");

        var tokenBytes = RandomNumberGenerator.GetBytes(32);
        var rawToken = Microsoft.AspNetCore.WebUtilities.Base64UrlTextEncoder.Encode(tokenBytes);
        var hashedToken = ComputeSha256(rawToken);

        var invitation = new Invitation
        {
            CompanyId = creator.CompanyId.Value,
            Email = command.Email.Trim(),
            TokenHash = hashedToken,
            CreatedById = createdById,
            ExpiresAt = DateTime.UtcNow.AddDays(ExpiryDays),
        };

        await _invitationRepo.AddAsync(invitation, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        string? inviteLink = null;
        if (!string.IsNullOrWhiteSpace(_frontendBaseUrl))
        {
            var baseUrl = _frontendBaseUrl.TrimEnd('/');
            inviteLink = $"{baseUrl}/accept-invite?token={rawToken}";
        }

        return new InviteUserResponse(rawToken, inviteLink);
    }

    private static string ComputeSha256(string input)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(input);
        var hash = SHA256.HashData(bytes);
        return Convert.ToBase64String(hash);
    }
}
