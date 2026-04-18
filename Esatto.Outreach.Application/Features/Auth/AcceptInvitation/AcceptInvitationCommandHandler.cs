using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Application.Abstractions.Services;
using Esatto.Outreach.Application.Features.Auth.Shared;
using Esatto.Outreach.Domain.Entities;
using Esatto.Outreach.Domain.Enums;
using Esatto.Outreach.Domain.Exceptions;
using Microsoft.AspNetCore.Identity;
using RefreshTokenEntity = Esatto.Outreach.Domain.Entities.RefreshToken;

namespace Esatto.Outreach.Application.Features.Auth.AcceptInvitation;

/// <summary>
/// Accepts an invitation: creates a new user or links existing user to the company, then returns JWT tokens.
/// </summary>
public sealed class AcceptInvitationCommandHandler
{
    private readonly IInvitationRepository _invitationRepo;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IJwtTokenService _jwtService;
    private readonly IRefreshTokenRepository _refreshTokenRepo;
    private readonly IOutreachPromptRepository _promptRepo;
    private readonly IUnitOfWork _unitOfWork;

    private const string DEFAULT_PROMPT = @"Fokusera på hur vi kan hjälpa företaget.
Använd informationen ovan om Esatto för att:
- Hitta relevanta cases som liknar kundens bransch eller utmaningar
- Visa konkret förståelse för kundens behov genom att referera till liknande projekt
- Matcha rätt tjänster och metoder till kundens situation
- Skriv i Esattos ton och värderingar (ärlighet, engagemang, omtanke, samarbete)
- Använd inga '-', inga bindestreck

Krav:
- Hook i första meningen.
- 1–2 konkreta värdeförslag anpassade till företaget.
- Referera gärna till ett eller två relevant Esatto-case som exempel
- Avsluta med en enkel call-to-action (t.ex. 'Vill du att jag skickar ett konkret förslag?').";

    public AcceptInvitationCommandHandler(
        IInvitationRepository invitationRepo,
        UserManager<ApplicationUser> userManager,
        IJwtTokenService jwtService,
        IRefreshTokenRepository refreshTokenRepo,
        IOutreachPromptRepository promptRepo,
        IUnitOfWork unitOfWork)
    {
        _invitationRepo = invitationRepo;
        _userManager = userManager;
        _jwtService = jwtService;
        _refreshTokenRepo = refreshTokenRepo;
        _promptRepo = promptRepo;
        _unitOfWork = unitOfWork;
    }

    public async Task<AuthResponseDto> Handle(
        AcceptInvitationRequest request,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Token))
            throw new AuthenticationFailedException("Invalid or expired invitation");

        var hashedToken = ComputeSha256(request.Token.Trim());

        var invitation = await _invitationRepo.GetByTokenAsync(hashedToken, ct);
        if (invitation == null)
            throw new AuthenticationFailedException("Invalid or expired invitation");
        if (invitation.UsedAt != null)
            throw new AuthenticationFailedException("Invalid or expired invitation");
        if (invitation.ExpiresAt < DateTime.UtcNow)
            throw new AuthenticationFailedException("Invalid or expired invitation");

        if (!string.Equals(request.Email, invitation.Email, StringComparison.OrdinalIgnoreCase))
            throw new AuthenticationFailedException("Invalid or expired invitation");

        var existingUser = await _userManager.FindByEmailAsync(invitation.Email);
        if (existingUser != null)
            throw new InvalidOperationException("User already exists in the system.");

        await using var transaction = await _unitOfWork.BeginTransactionAsync(ct);
        try
        {
            var user = new ApplicationUser
            {
                UserName = invitation.Email,
                Email = invitation.Email,
                FullName = request.FullName,
                CreatedUtc = DateTime.UtcNow,
                CompanyId = invitation.CompanyId,
            };

            var createResult = await _userManager.CreateAsync(user, request.Password);
            if (!createResult.Succeeded)
            {
                var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Registration failed: {errors}");
            }

            var defaultGeneral = OutreachPrompt.Create(user.Id, DEFAULT_PROMPT, PromptType.General, isActive: true);
            var defaultEmail = OutreachPrompt.Create(user.Id, DEFAULT_PROMPT, PromptType.Email, isActive: true);
            var defaultLinkedIn = OutreachPrompt.Create(user.Id, DEFAULT_PROMPT, PromptType.LinkedIn, isActive: true);
            await _promptRepo.AddAsync(defaultGeneral, ct);
            await _promptRepo.AddAsync(defaultEmail, ct);
            await _promptRepo.AddAsync(defaultLinkedIn, ct);

            if (!await _invitationRepo.MarkAsUsedAsync(invitation.Id, ct))
            {
                throw new AuthenticationFailedException("Invalid or expired invitation");
            }

            var (accessToken, expiresAt) = _jwtService.GenerateAccessToken(user);
            var refreshToken = _jwtService.GenerateRefreshToken();

            await _refreshTokenRepo.AddAsync(new RefreshTokenEntity
            {
                TokenHash = RefreshTokenEntity.ComputeTokenHash(refreshToken),
                UserId = user.Id,
                ExpiresAt = _jwtService.GetRefreshTokenExpiryDate()
            }, ct);

            await _unitOfWork.CommitTransactionAsync(ct);

            return new AuthResponseDto(
                accessToken,
                refreshToken,
                expiresAt,
                new UserDto(user.Id, user.Email!, user.FullName)
            );
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(ct);
            throw;
        }
    }

    private static string ComputeSha256(string input)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(input);
        var hash = System.Security.Cryptography.SHA256.HashData(bytes);
        return Convert.ToBase64String(hash);
    }
}
