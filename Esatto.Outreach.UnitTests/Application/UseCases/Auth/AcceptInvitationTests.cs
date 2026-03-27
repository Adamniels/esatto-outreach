using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Application.Abstractions.Services;
using Esatto.Outreach.Application.DTOs.Auth;
using Esatto.Outreach.Application.UseCases.Auth;
using Esatto.Outreach.Domain.Entities;
using Esatto.Outreach.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using NSubstitute;
using Xunit;

namespace Esatto.Outreach.UnitTests.Application.UseCases.Auth;

public class AcceptInvitationTests
{
    private readonly IInvitationRepository _invitationRepo;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IJwtTokenService _jwt;
    private readonly IRefreshTokenRepository _refreshTokenRepo;
    private readonly IOutreachPromptRepository _promptRepo;
    private readonly AcceptInvitation _useCase;

    public AcceptInvitationTests()
    {
        _invitationRepo = Substitute.For<IInvitationRepository>();
        _jwt = Substitute.For<IJwtTokenService>();
        _refreshTokenRepo = Substitute.For<IRefreshTokenRepository>();
        _promptRepo = Substitute.For<IOutreachPromptRepository>();

        var userStore = Substitute.For<IUserStore<ApplicationUser>>();
        _userManager = Substitute.For<UserManager<ApplicationUser>>(
            userStore, null!, null!, null!, null!, null!, null!, null!, null!);

        _useCase = new AcceptInvitation(
            _invitationRepo, _userManager, _jwt, _refreshTokenRepo, _promptRepo);
    }

    // AcceptInvitationDto has 4 params: Token, Email, Password, FullName
    private static AcceptInvitationDto Req(string token = "tok", string email = "user@esatto.se",
        string password = "Pass1234", string? fullName = "Test User")
        => new(token, email, password, fullName);

    private static string Hash(string raw)
    {
        var bytes = Encoding.UTF8.GetBytes(raw.Trim());
        return Convert.ToBase64String(SHA256.HashData(bytes));
    }

    private static Invitation ValidInvitation(string email = "user@esatto.se") => new()
    {
        Email = email,
        TokenHash = Hash("tok"),
        ExpiresAt = DateTime.UtcNow.AddDays(1),
        CompanyId = Guid.NewGuid(),
        CreatedById = "admin-id",
    };

    // --- Rejection paths ---

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Handle_WithBlankToken_RejectsWithoutHittingDatabase(string? token)
    {
        var result = await _useCase.Handle(Req(token!));

        result.Success.Should().BeFalse();
        result.Error.Should().Be("Invalid or expired invitation");
        await _invitationRepo.DidNotReceive().GetByTokenAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithTokenNotInDatabase_ReturnsFalse()
    {
        _invitationRepo.GetByTokenAsync(Hash("tok"), Arg.Any<CancellationToken>())
            .Returns((Invitation?)null);

        var result = await _useCase.Handle(Req());

        result.Success.Should().BeFalse();
        result.Error.Should().Be("Invalid or expired invitation");
    }

    [Fact]
    public async Task Handle_WithAlreadyUsedInvitation_ReturnsFalse()
    {
        var invitation = ValidInvitation();
        invitation.UsedAt = DateTime.UtcNow.AddHours(-1); // already used
        _invitationRepo.GetByTokenAsync(Hash("tok"), Arg.Any<CancellationToken>()).Returns(invitation);

        var result = await _useCase.Handle(Req());

        result.Success.Should().BeFalse();
        result.Error.Should().Be("Invalid or expired invitation");
    }

    [Fact]
    public async Task Handle_WithExpiredInvitation_ReturnsFalse()
    {
        var invitation = ValidInvitation();
        invitation.ExpiresAt = DateTime.UtcNow.AddMinutes(-1); // expired
        _invitationRepo.GetByTokenAsync(Hash("tok"), Arg.Any<CancellationToken>()).Returns(invitation);

        var result = await _useCase.Handle(Req());

        result.Success.Should().BeFalse();
        result.Error.Should().Be("Invalid or expired invitation");
    }

    [Fact]
    public async Task Handle_WithMismatchedEmail_ReturnsFalse()
    {
        // Invitation is for user@esatto.se but request claims attacker@evil.com
        _invitationRepo.GetByTokenAsync(Hash("tok"), Arg.Any<CancellationToken>())
            .Returns(ValidInvitation("user@esatto.se"));

        var result = await _useCase.Handle(Req(email: "attacker@evil.com"));

        result.Success.Should().BeFalse();
        result.Error.Should().Be("Invalid or expired invitation");
        // This is the critical security gate — an attacker cannot claim someone else's invite
    }

    [Fact]
    public async Task Handle_WhenUserAlreadyExists_ReturnsFalse()
    {
        _invitationRepo.GetByTokenAsync(Hash("tok"), Arg.Any<CancellationToken>())
            .Returns(ValidInvitation());
        _userManager.FindByEmailAsync("user@esatto.se")
            .Returns(new ApplicationUser { Email = "user@esatto.se" });

        var result = await _useCase.Handle(Req());

        result.Success.Should().BeFalse();
        result.Error.Should().Be("User already exists in the system.");
    }

    [Fact]
    public async Task Handle_WhenIdentityCreateFails_ReturnsRegistrationErrors()
    {
        _invitationRepo.GetByTokenAsync(Hash("tok"), Arg.Any<CancellationToken>())
            .Returns(ValidInvitation());
        _userManager.FindByEmailAsync(Arg.Any<string>()).Returns((ApplicationUser?)null);
        _userManager.CreateAsync(Arg.Any<ApplicationUser>(), Arg.Any<string>())
            .Returns(IdentityResult.Failed(new IdentityError { Description = "Password too weak" }));

        var result = await _useCase.Handle(Req());

        result.Success.Should().BeFalse();
        result.Error.Should().Contain("Password too weak");
    }

    // --- Success path ---

    [Fact]
    public async Task Handle_WithValidInvitation_CreatesUserIssuesTokensAndMarksInvitationUsed()
    {
        var invitation = ValidInvitation();
        var invitationId = invitation.Id; // Id is Guid.NewGuid() from Entity base

        _invitationRepo.GetByTokenAsync(Hash("tok"), Arg.Any<CancellationToken>()).Returns(invitation);
        _userManager.FindByEmailAsync(Arg.Any<string>()).Returns((ApplicationUser?)null);
        _userManager.CreateAsync(Arg.Any<ApplicationUser>(), Arg.Any<string>())
            .Returns(IdentityResult.Success);
        _invitationRepo.MarkAsUsedAsync(invitationId, Arg.Any<CancellationToken>()).Returns(true);
        _jwt.GenerateAccessToken(Arg.Any<ApplicationUser>())
            .Returns(("access-token-xyz", DateTime.UtcNow.AddHours(1)));
        _jwt.GenerateRefreshToken().Returns("refresh-token-xyz");
        _jwt.GetRefreshTokenExpiryDate().Returns(DateTime.UtcNow.AddDays(7));

        var result = await _useCase.Handle(Req());

        result.Success.Should().BeTrue();
        result.Error.Should().BeNull();
        result.Data!.AccessToken.Should().Be("access-token-xyz");
        result.Data.RefreshToken.Should().Be("refresh-token-xyz");

        // Invitation must be marked as used — prevents replay
        await _invitationRepo.Received(1).MarkAsUsedAsync(invitationId, Arg.Any<CancellationToken>());

        // 3 default prompts must be seeded (General, Email, LinkedIn)
        await _promptRepo.Received(3).AddAsync(Arg.Any<OutreachPrompt>(), Arg.Any<CancellationToken>());

        // A refresh token must be persisted
        await _refreshTokenRepo.Received(1)
            .AddAsync(Arg.Is<RefreshToken>(rt => rt.Token == "refresh-token-xyz"), Arg.Any<CancellationToken>());
    }
}
