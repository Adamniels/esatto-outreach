using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Application.Abstractions.Services;
using Esatto.Outreach.Application.Features.Auth;
using Esatto.Outreach.Domain.Entities;
using Esatto.Outreach.Domain.Exceptions;
using Microsoft.AspNetCore.Identity;

namespace Esatto.Outreach.Application.Features.Auth;

/// <summary>
/// Refresh an expired access token using a refresh token.
/// </summary>
public sealed class RefreshAccessTokenCommandHandler
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IJwtTokenService _jwtService;
    private readonly IRefreshTokenRepository _refreshTokenRepo;

    public RefreshAccessTokenCommandHandler(
        UserManager<ApplicationUser> userManager,
        IJwtTokenService jwtService,
        IRefreshTokenRepository refreshTokenRepo)
    {
        _userManager = userManager;
        _jwtService = jwtService;
        _refreshTokenRepo = refreshTokenRepo;
    }

    public async Task<AuthResponseDto> Handle(
        RefreshTokenRequestDto request,
        CancellationToken ct = default)
    {
        // Find refresh token in database
        var refreshToken = await _refreshTokenRepo.GetByTokenAsync(request.RefreshToken, ct);

        if (refreshToken == null)
            throw new AuthenticationFailedException("Invalid refresh token");

        if (refreshToken.IsRevoked)
            throw new AuthenticationFailedException("Refresh token has been revoked");

        if (refreshToken.ExpiresAt < DateTime.UtcNow)
            throw new AuthenticationFailedException("Refresh token has expired");

        // Generate new tokens
        var (accessToken, expiresAt) = _jwtService.GenerateAccessToken(refreshToken.User);
        var newRefreshToken = _jwtService.GenerateRefreshToken();

        // Revoke old refresh token
        refreshToken.IsRevoked = true;
        await _refreshTokenRepo.UpdateAsync(refreshToken, ct);

        // Store new refresh token
        await _refreshTokenRepo.AddAsync(new RefreshToken
        {
            Token = newRefreshToken,
            UserId = refreshToken.UserId,
            ExpiresAt = _jwtService.GetRefreshTokenExpiryDate()
        }, ct);

        return new AuthResponseDto(
            accessToken,
            newRefreshToken,
            expiresAt,
            new UserDto(refreshToken.User.Id, refreshToken.User.Email!, refreshToken.User.FullName)
        );
    }
}
