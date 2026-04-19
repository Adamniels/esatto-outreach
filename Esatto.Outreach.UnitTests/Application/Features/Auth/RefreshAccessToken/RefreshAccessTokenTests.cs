using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Application.Abstractions.Services;
using Esatto.Outreach.Application.Features.Auth.RefreshToken;
using Esatto.Outreach.Domain.Entities;
using Esatto.Outreach.Domain.Exceptions;
using Esatto.Outreach.UnitTests.Helpers;
using FluentAssertions;
using NSubstitute;

namespace Esatto.Outreach.UnitTests.Application.Features.Auth.RefreshAccessToken;

public class RefreshAccessTokenTests
{
    private readonly IRefreshTokenRepository _repoMock;
    private readonly IJwtTokenService _jwtMock;
    private readonly IUnitOfWork _unitOfWorkMock;
    private readonly RefreshAccessTokenCommandHandler _useCase;

    public RefreshAccessTokenTests()
    {
        _repoMock = Substitute.For<IRefreshTokenRepository>();
        _jwtMock = Substitute.For<IJwtTokenService>();
        _unitOfWorkMock = Substitute.For<IUnitOfWork>();

        // UserManager is injected but currently unused in the RefreshAccessTokenCommandHandler flow,
        // so we can safely pass null or a dummy mock.
        _useCase = new RefreshAccessTokenCommandHandler(null!, _jwtMock, _repoMock, _unitOfWorkMock);
    }

    [Fact]
    public async Task Handle_WithInvalidToken_ThrowsAuthenticationFailedException()
    {
        // Arrange
        var req = new RefreshAccessTokenCommand("invalid-token");
        _repoMock.GetByTokenAsync(req.RefreshToken, Arg.Any<CancellationToken>())
            .Returns((RefreshToken?)null);

        // Act & Assert
        Func<Task> act = async () => await _useCase.Handle(req);
        await act.Should().ThrowAsync<AuthenticationFailedException>()
            .WithMessage("Invalid refresh token");
    }

    [Fact]
    public async Task Handle_WithRevokedToken_ThrowsAuthenticationFailedException()
    {
        // Arrange
        var req = new RefreshAccessTokenCommand("revoked-token");
        var token = new RefreshToken { TokenHash = RefreshToken.ComputeTokenHash("revoked-token"), IsRevoked = true };

        _repoMock.GetByTokenAsync(req.RefreshToken, Arg.Any<CancellationToken>())
            .Returns(token);

        // Act & Assert
        Func<Task> act = async () => await _useCase.Handle(req);
        await act.Should().ThrowAsync<AuthenticationFailedException>()
            .WithMessage("Refresh token has been revoked");
    }

    [Fact]
    public async Task Handle_WithExpiredToken_ThrowsAuthenticationFailedException()
    {
        // Arrange
        var req = new RefreshAccessTokenCommand("expired-token");
        var token = new RefreshToken
        {
            TokenHash = RefreshToken.ComputeTokenHash("expired-token"),
            IsRevoked = false,
            ExpiresAt = DateTime.UtcNow.AddMinutes(-10) // Expired
        };

        _repoMock.GetByTokenAsync(req.RefreshToken, Arg.Any<CancellationToken>())
            .Returns(token);

        // Act & Assert
        Func<Task> act = async () => await _useCase.Handle(req);
        await act.Should().ThrowAsync<AuthenticationFailedException>()
            .WithMessage("Refresh token has expired");
    }

    [Fact]
    public async Task Handle_WithValidToken_RotatesTokenAndReturnsNewTokens()
    {
        // Arrange
        var req = new RefreshAccessTokenCommand("valid-token");
        var user = new ApplicationUser { Id = "user-1", Email = "test@test.com", FullName = "Test User" };
        var oldToken = new RefreshToken
        {
            TokenHash = RefreshToken.ComputeTokenHash("valid-token"),
            IsRevoked = false,
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            User = user,
            UserId = user.Id
        };

        _repoMock.GetByTokenAsync(req.RefreshToken, Arg.Any<CancellationToken>())
            .Returns(oldToken);

        _repoMock.TryRevokeActiveTokenAsync(Arg.Any<Guid>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(true);

        _jwtMock.GenerateAccessToken(user)
            .Returns(("new-access-token", DateTime.UtcNow.AddHours(1)));

        _jwtMock.GenerateRefreshToken().Returns("new-refresh-token");
        _jwtMock.GetRefreshTokenExpiryDate().Returns(DateTime.UtcNow.AddDays(7));

        // Act
        var result = await _useCase.Handle(req);

        // Assert
        ObjectAssertion.Should(result).NotBeNull();
        result.AccessToken.Should().Be("new-access-token");
        result.RefreshToken.Should().Be("new-refresh-token");

        await _repoMock.Received(1).TryRevokeActiveTokenAsync(
            oldToken.Id,
            Arg.Any<DateTime>(),
            Arg.Any<CancellationToken>());

        // Ensure new token was stored
        await _repoMock.Received(1).AddAsync(Arg.Is<RefreshToken>(t =>
            t.TokenHash == RefreshToken.ComputeTokenHash("new-refresh-token") &&
            t.UserId == "user-1"), Arg.Any<CancellationToken>());
        await _unitOfWorkMock.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenAtomicRevokeFails_ThrowsAuthenticationFailedException()
    {
        // Arrange — concurrent rotation already revoked the token at the database
        var req = new RefreshAccessTokenCommand("valid-token");
        var user = new ApplicationUser { Id = "user-1", Email = "test@test.com", FullName = "Test User" };
        var oldToken = new RefreshToken
        {
            TokenHash = RefreshToken.ComputeTokenHash("valid-token"),
            IsRevoked = false,
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            User = user,
            UserId = user.Id
        };

        _repoMock.GetByTokenAsync(req.RefreshToken, Arg.Any<CancellationToken>())
            .Returns(oldToken);

        _repoMock.TryRevokeActiveTokenAsync(Arg.Any<Guid>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(false);

        // Act & Assert
        Func<Task> act = async () => await _useCase.Handle(req);
        await act.Should().ThrowAsync<AuthenticationFailedException>()
            .WithMessage("Refresh token has already been used");

        await _repoMock.Received(1).TryRevokeActiveTokenAsync(
            oldToken.Id,
            Arg.Any<DateTime>(),
            Arg.Any<CancellationToken>());
        await _repoMock.DidNotReceive().AddAsync(Arg.Any<RefreshToken>(), Arg.Any<CancellationToken>());
    }
}
