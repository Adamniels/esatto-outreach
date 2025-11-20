using Esatto.Outreach.Application.Abstractions;
using Esatto.Outreach.Application.DTOs.Auth;
using Esatto.Outreach.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace Esatto.Outreach.Application.UseCases.Auth;

/// <summary>
/// Login an existing user and return JWT tokens.
/// </summary>
public sealed class Login
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IJwtTokenService _jwtService;
    private readonly IRefreshTokenRepository _refreshTokenRepo;

    public Login(
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

    public async Task<(bool Success, AuthResponseDto? Data, string? Error)> Handle(
        LoginRequestDto request,
        CancellationToken ct = default)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
            return (false, null, "Invalid email or password");

        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
        if (!result.Succeeded)
            return (false, null, "Invalid email or password");

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

        var response = new AuthResponseDto(
            accessToken,
            refreshToken,
            expiresAt,
            new UserDto(user.Id, user.Email!, user.FullName)
        );

        return (true, response, null);
    }
}
