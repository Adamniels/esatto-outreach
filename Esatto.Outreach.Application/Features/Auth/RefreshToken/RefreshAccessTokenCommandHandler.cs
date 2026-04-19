using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Application.Abstractions.Services;
using Esatto.Outreach.Application.Features.Auth.Shared;
using Esatto.Outreach.Domain.Entities;
using Esatto.Outreach.Domain.Exceptions;
using Microsoft.AspNetCore.Identity;
using RefreshTokenEntity = Esatto.Outreach.Domain.Entities.RefreshToken;

namespace Esatto.Outreach.Application.Features.Auth.RefreshToken;

public sealed class RefreshAccessTokenCommandHandler
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IJwtTokenService _jwtService;
    private readonly IRefreshTokenRepository _refreshTokenRepo;
    private readonly IUnitOfWork _unitOfWork;

    public RefreshAccessTokenCommandHandler(
        UserManager<ApplicationUser> userManager,
        IJwtTokenService jwtService,
        IRefreshTokenRepository refreshTokenRepo,
        IUnitOfWork unitOfWork)
    {
        _userManager = userManager;
        _jwtService = jwtService;
        _refreshTokenRepo = refreshTokenRepo;
        _unitOfWork = unitOfWork;
    }

    public async Task<AuthResponseDto> Handle(
        RefreshAccessTokenCommand command,
        CancellationToken ct = default)
    {
        await _unitOfWork.BeginTransactionAsync(ct);
        try
        {
        var utcNow = DateTime.UtcNow;

        var refreshToken = await _refreshTokenRepo.GetByTokenAsync(command.RefreshToken, ct);

        if (refreshToken == null)
            throw new AuthenticationFailedException("Invalid refresh token");

        if (refreshToken.IsRevoked)
            throw new AuthenticationFailedException("Refresh token has been revoked");

        if (refreshToken.ExpiresAt < utcNow)
            throw new AuthenticationFailedException("Refresh token has expired");

        var revoked = await _refreshTokenRepo.TryRevokeActiveTokenAsync(refreshToken.Id, utcNow, ct);
        if (!revoked)
            throw new AuthenticationFailedException("Refresh token has already been used");

        var (accessToken, expiresAt) = _jwtService.GenerateAccessToken(refreshToken.User);
        var newRefreshToken = _jwtService.GenerateRefreshToken();

        await _refreshTokenRepo.AddAsync(new RefreshTokenEntity
        {
            TokenHash = RefreshTokenEntity.ComputeTokenHash(newRefreshToken),
            UserId = refreshToken.UserId,
            ExpiresAt = _jwtService.GetRefreshTokenExpiryDate()
        }, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        await _unitOfWork.CommitTransactionAsync(ct);

        return new AuthResponseDto(
            accessToken,
            newRefreshToken,
            expiresAt,
            new UserDto(refreshToken.User.Id, refreshToken.User.Email!, refreshToken.User.FullName)
        );
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(ct);
            throw;
        }
    }
}
