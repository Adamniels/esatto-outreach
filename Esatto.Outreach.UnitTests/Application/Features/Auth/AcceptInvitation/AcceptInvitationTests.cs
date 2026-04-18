using System.Security.Cryptography;
using System.Text;
using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Application.Abstractions.Services;
using Esatto.Outreach.Application.Features.Auth.AcceptInvitation;
using Esatto.Outreach.Domain.Entities;
using Esatto.Outreach.Domain.Exceptions;
using Esatto.Outreach.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using NSubstitute;

namespace Esatto.Outreach.UnitTests.Application.Features.Auth.AcceptInvitation;

public class AcceptInvitationTests
{
    private readonly IInvitationRepository _invitationRepo;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IJwtTokenService _jwt;
    private readonly IRefreshTokenRepository _refreshTokenRepo;
    private readonly IOutreachPromptRepository _promptRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly AcceptInvitationCommandHandler _useCase;

    public AcceptInvitationTests()
    {
        _invitationRepo = Substitute.For<IInvitationRepository>();
        _jwt = Substitute.For<IJwtTokenService>();
        _refreshTokenRepo = Substitute.For<IRefreshTokenRepository>();
        _promptRepo = Substitute.For<IOutreachPromptRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();

        var userStore = Substitute.For<IUserStore<ApplicationUser>>();
        _userManager = Substitute.For<UserManager<ApplicationUser>>(
            userStore, null!, null!, null!, null!, null!, null!, null!, null!);

        _useCase = new AcceptInvitationCommandHandler(
            _invitationRepo, _userManager, _jwt, _refreshTokenRepo, _promptRepo, _unitOfWork);
    }

    // AcceptInvitationRequest has 4 params: Token, Email, Password, FullName
    private static AcceptInvitationRequest Req(string token = "tok", string email = "user@esatto.se",
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
    public async Task Handle_WithBlankToken_ThrowsWithoutHittingDatabase(string? token)
    {
        Func<Task> act = async () => await _useCase.Handle(Req(token!));

        await act.Should().ThrowAsync<AuthenticationFailedException>()
            .WithMessage("Invalid or expired invitation");
        await _invitationRepo.DidNotReceive().GetByTokenAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithTokenNotInDatabase_ThrowsAuthenticationFailedException()
    {
        _invitationRepo.GetByTokenAsync(Hash("tok"), Arg.Any<CancellationToken>())
            .Returns((Invitation?)null);

        Func<Task> act = async () => await _useCase.Handle(Req());

        await act.Should().ThrowAsync<AuthenticationFailedException>()
            .WithMessage("Invalid or expired invitation");
    }

    [Fact]
    public async Task Handle_WithAlreadyUsedInvitation_ThrowsAuthenticationFailedException()
    {
        var invitation = ValidInvitation();
        invitation.UsedAt = DateTime.UtcNow.AddHours(-1); // already used
        _invitationRepo.GetByTokenAsync(Hash("tok"), Arg.Any<CancellationToken>()).Returns(invitation);

        Func<Task> act = async () => await _useCase.Handle(Req());

        await act.Should().ThrowAsync<AuthenticationFailedException>()
            .WithMessage("Invalid or expired invitation");
    }

    [Fact]
    public async Task Handle_WithExpiredInvitation_ThrowsAuthenticationFailedException()
    {
        var invitation = ValidInvitation();
        invitation.ExpiresAt = DateTime.UtcNow.AddMinutes(-1); // expired
        _invitationRepo.GetByTokenAsync(Hash("tok"), Arg.Any<CancellationToken>()).Returns(invitation);

        Func<Task> act = async () => await _useCase.Handle(Req());

        await act.Should().ThrowAsync<AuthenticationFailedException>()
            .WithMessage("Invalid or expired invitation");
    }

    [Fact]
    public async Task Handle_WithMismatchedEmail_ThrowsAuthenticationFailedException()
    {
        // Invitation is for user@esatto.se but request claims attacker@evil.com
        _invitationRepo.GetByTokenAsync(Hash("tok"), Arg.Any<CancellationToken>())
            .Returns(ValidInvitation("user@esatto.se"));

        Func<Task> act = async () => await _useCase.Handle(Req(email: "attacker@evil.com"));

        await act.Should().ThrowAsync<AuthenticationFailedException>()
            .WithMessage("Invalid or expired invitation");
        // This is the critical security gate — an attacker cannot claim someone else's invite
    }

    [Fact]
    public async Task Handle_WhenUserAlreadyExists_ThrowsInvalidOperationException()
    {
        _invitationRepo.GetByTokenAsync(Hash("tok"), Arg.Any<CancellationToken>())
            .Returns(ValidInvitation());
        _userManager.FindByEmailAsync("user@esatto.se")
            .Returns(new ApplicationUser { Email = "user@esatto.se" });

        Func<Task> act = async () => await _useCase.Handle(Req());

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("User already exists in the system.");
    }

    [Fact]
    public async Task Handle_WhenIdentityCreateFails_ThrowsInvalidOperationException()
    {
        _invitationRepo.GetByTokenAsync(Hash("tok"), Arg.Any<CancellationToken>())
            .Returns(ValidInvitation());
        _userManager.FindByEmailAsync(Arg.Any<string>()).Returns((ApplicationUser?)null);
        _userManager.CreateAsync(Arg.Any<ApplicationUser>(), Arg.Any<string>())
            .Returns(IdentityResult.Failed(new IdentityError { Description = "Password too weak" }));

        Func<Task> act = async () => await _useCase.Handle(Req());

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Password too weak*");
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

        ObjectAssertion.Should(result).NotBeNull();
        result.AccessToken.Should().Be("access-token-xyz");
        result.RefreshToken.Should().Be("refresh-token-xyz");

        // Invitation must be marked as used — prevents replay
        await _invitationRepo.Received(1).MarkAsUsedAsync(invitationId, Arg.Any<CancellationToken>());

        // 3 default prompts must be seeded (General, Email, LinkedIn)
        await _promptRepo.Received(3).AddAsync(Arg.Any<OutreachPrompt>(), Arg.Any<CancellationToken>());

        // A refresh token must be persisted
        await _refreshTokenRepo.Received(1)
            .AddAsync(Arg.Is<RefreshToken>(rt => rt.TokenHash == RefreshToken.ComputeTokenHash("refresh-token-xyz")), Arg.Any<CancellationToken>());
    }
}
