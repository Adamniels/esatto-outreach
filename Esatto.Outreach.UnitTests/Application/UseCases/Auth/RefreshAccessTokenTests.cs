using System;
using System.Threading;
using System.Threading.Tasks;
using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Application.Abstractions.Services;
using Esatto.Outreach.Application.DTOs.Auth;
using Esatto.Outreach.Application.UseCases.Auth;
using Esatto.Outreach.Domain.Entities;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using NSubstitute;
using Xunit;

namespace Esatto.Outreach.UnitTests.Application.UseCases.Auth;

public class RefreshAccessTokenTests
{
    private readonly IRefreshTokenRepository _repoMock;
    private readonly IJwtTokenService _jwtMock;
    private readonly RefreshAccessToken _useCase;

    public RefreshAccessTokenTests()
    {
        _repoMock = Substitute.For<IRefreshTokenRepository>();
        _jwtMock = Substitute.For<IJwtTokenService>();

        // UserManager is injected but currently unused in the RefreshAccessToken flow,
        // so we can safely pass null or a dummy mock.
        _useCase = new RefreshAccessToken(null!, _jwtMock, _repoMock);
    }

    [Fact]
    public async Task Handle_WithInvalidToken_ReturnsFalseAndError()
    {
        // Arrange
        var req = new RefreshTokenRequestDto("invalid-token");
        _repoMock.GetByTokenAsync(req.RefreshToken, Arg.Any<CancellationToken>())
            .Returns((RefreshToken?)null);

        // Act
        var result = await _useCase.Handle(req);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Be("Invalid refresh token");
        result.Data.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WithRevokedToken_ReturnsFalseAndError()
    {
        // Arrange
        var req = new RefreshTokenRequestDto("revoked-token");
        var token = new RefreshToken { Token = "revoked-token", IsRevoked = true };
        
        _repoMock.GetByTokenAsync(req.RefreshToken, Arg.Any<CancellationToken>())
            .Returns(token);

        // Act
        var result = await _useCase.Handle(req);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Be("Refresh token has been revoked");
        result.Data.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WithExpiredToken_ReturnsFalseAndError()
    {
        // Arrange
        var req = new RefreshTokenRequestDto("expired-token");
        var token = new RefreshToken 
        { 
            Token = "expired-token", 
            IsRevoked = false, 
            ExpiresAt = DateTime.UtcNow.AddMinutes(-10) // Expired
        };
        
        _repoMock.GetByTokenAsync(req.RefreshToken, Arg.Any<CancellationToken>())
            .Returns(token);

        // Act
        var result = await _useCase.Handle(req);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Be("Refresh token has expired");
        result.Data.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WithValidToken_RotatesTokenAndReturnsNewTokens()
    {
        // Arrange
        var req = new RefreshTokenRequestDto("valid-token");
        var user = new ApplicationUser { Id = "user-1", Email = "test@test.com", FullName = "Test User" };
        var oldToken = new RefreshToken 
        { 
            Token = "valid-token", 
            IsRevoked = false, 
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            User = user,
            UserId = user.Id
        };
        
        _repoMock.GetByTokenAsync(req.RefreshToken, Arg.Any<CancellationToken>())
            .Returns(oldToken);

        _jwtMock.GenerateAccessToken(user)
            .Returns(("new-access-token", DateTime.UtcNow.AddHours(1)));
            
        _jwtMock.GenerateRefreshToken().Returns("new-refresh-token");
        _jwtMock.GetRefreshTokenExpiryDate().Returns(DateTime.UtcNow.AddDays(7));

        // Act
        var result = await _useCase.Handle(req);

        // Assert
        result.Success.Should().BeTrue();
        result.Error.Should().BeNull();
        result.Data.Should().NotBeNull();
        result.Data!.AccessToken.Should().Be("new-access-token");
        result.Data.RefreshToken.Should().Be("new-refresh-token");
        
        // Ensure old token was revoked
        oldToken.IsRevoked.Should().BeTrue();
        await _repoMock.Received(1).UpdateAsync(oldToken, Arg.Any<CancellationToken>());
        
        // Ensure new token was stored
        await _repoMock.Received(1).AddAsync(Arg.Is<RefreshToken>(t => 
            t.Token == "new-refresh-token" && 
            t.UserId == "user-1"), Arg.Any<CancellationToken>());
    }
}
