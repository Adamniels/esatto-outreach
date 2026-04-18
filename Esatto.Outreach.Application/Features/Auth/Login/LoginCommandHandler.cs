using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Application.Abstractions.Services;
using Esatto.Outreach.Application.Features.Auth;
using Esatto.Outreach.Domain.Entities;
using Esatto.Outreach.Domain.Exceptions;
using Microsoft.AspNetCore.Identity;

namespace Esatto.Outreach.Application.Features.Auth;

/// <summary>
/// LoginCommandHandler an existing user and return JWT tokens.
/// </summary>
public sealed class LoginCommandHandler
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IJwtTokenService _jwtService;
    private readonly IRefreshTokenRepository _refreshTokenRepo;

    public LoginCommandHandler(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IJwtTokenService jwtService,
        IRefreshTokenRepository refreshTokenRepo)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _jwtService = jwtService;
        _refreshTokenRepo = refreshTokenRepo;
    }

    public async Task<AuthResponseDto> Handle(
        LoginRequestDto request,
        CancellationToken ct = default)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
            throw new AuthenticationFailedException("Invalid email or password");

        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
        if (!result.Succeeded)
            throw new AuthenticationFailedException("Invalid email or password");

        // Update last login
        user.LastLoginUtc = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        // Generate tokens
        var (accessToken, expiresAt) = _jwtService.GenerateAccessToken(user);
        var refreshToken = _jwtService.GenerateRefreshToken();

        // Store new refresh token
        await _refreshTokenRepo.AddAsync(new RefreshToken
        {
            Token = refreshToken,
            UserId = user.Id,
            ExpiresAt = _jwtService.GetRefreshTokenExpiryDate()
        }, ct);

        return new AuthResponseDto(
            accessToken,
            refreshToken,
            expiresAt,
            new UserDto(user.Id, user.Email!, user.FullName)
        );
    }
}
