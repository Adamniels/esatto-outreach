using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Application.Abstractions.Services;
using Esatto.Outreach.Application.Features.Auth.Shared;
using Esatto.Outreach.Domain.Entities;
using Esatto.Outreach.Domain.Exceptions;
using Microsoft.AspNetCore.Identity;
using RefreshTokenEntity = Esatto.Outreach.Domain.Entities.RefreshToken;

namespace Esatto.Outreach.Application.Features.Auth.LoginUser;

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
        LoginRequest request,
        CancellationToken ct = default)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
            throw new AuthenticationFailedException("Invalid email or password");

        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
        if (!result.Succeeded)
            throw new AuthenticationFailedException("Invalid email or password");

        user.LastLoginUtc = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        var (accessToken, expiresAt) = _jwtService.GenerateAccessToken(user);
        var refreshToken = _jwtService.GenerateRefreshToken();

        await _refreshTokenRepo.AddAsync(new RefreshTokenEntity
        {
            TokenHash = RefreshTokenEntity.ComputeTokenHash(refreshToken),
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
