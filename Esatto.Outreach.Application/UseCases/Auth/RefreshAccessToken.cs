using Esatto.Outreach.Application.Abstractions;
using Esatto.Outreach.Application.DTOs.Auth;
using Esatto.Outreach.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace Esatto.Outreach.Application.UseCases.Auth;

/// <summary>
/// Refresh an expired access token using a refresh token.
/// </summary>
public sealed class RefreshAccessToken
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IJwtTokenService _jwtService;
    private readonly IRefreshTokenRepository _refreshTokenRepo;

    public RefreshAccessToken(
        UserManager<ApplicationUser> userManager,
        IJwtTokenService jwtService,
        IRefreshTokenRepository refreshTokenRepo)
    {
        _userManager = userManager;
        _jwtService = jwtService;
        _refreshTokenRepo = refreshTokenRepo;
    }

    public async Task<(bool Success, AuthResponseDto? Data, string? Error)> Handle(
        RefreshTokenRequestDto request,
        CancellationToken ct = default)
    {
        // Find refresh token in database
        var refreshToken = await _refreshTokenRepo.GetByTokenAsync(request.RefreshToken, ct);

        if (refreshToken == null)
            return (false, null, "Invalid refresh token");

        if (refreshToken.IsRevoked)
            return (false, null, "Refresh token has been revoked");

        if (refreshToken.ExpiresAt < DateTime.UtcNow)
            return (false, null, "Refresh token has expired");

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

        var response = new AuthResponseDto(
            accessToken,
            newRefreshToken,
            expiresAt,
            new UserDto(refreshToken.User.Id, refreshToken.User.Email!, refreshToken.User.FullName)
        );

        return (true, response, null);
    }
}
