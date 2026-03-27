using System;
using System.Threading;
using System.Threading.Tasks;
using Esatto.Outreach.Application.Abstractions.Clients;
using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Application.DTOs.Intelligence;
using Esatto.Outreach.Application.UseCases.Intelligence;
using Esatto.Outreach.Domain.Entities;
using Esatto.Outreach.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Esatto.Outreach.UnitTests.Application.UseCases.Intelligence;

public class ChatWithProspectTests
{
    private readonly IProspectRepository _repo;
    private readonly IEntityIntelligenceRepository _enrichmentRepo;
    private readonly IOpenAIChatClient _chat;
    private readonly ChatWithProspect _useCase;

    public ChatWithProspectTests()
    {
        _repo = Substitute.For<IProspectRepository>();
        _enrichmentRepo = Substitute.For<IEntityIntelligenceRepository>();
        _chat = Substitute.For<IOpenAIChatClient>();

        _useCase = new ChatWithProspect(
            _repo,
            _enrichmentRepo,
            _chat,
            Substitute.For<ILogger<ChatWithProspect>>());
    }

    private static ChatRequestDto DefaultRequest()
        => new("Hello AI", null, null, false, 0.7, 1000);

    [Fact]
    public async Task Handle_WhenProspectNotFound_ThrowsInvalidOperationException()
    {
        _repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Prospect?)null);

        var act = () => _useCase.Handle(Guid.NewGuid(), "any-user", DefaultRequest());

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Prospect not found*");
    }

    [Fact]
    public async Task Handle_WhenUserIsNotOwner_ThrowsUnauthorizedAccessException()
    {
        // Arrange: prospect owned by "user-A"
        var prospect = TestFactory.CreateValidManualProspect(ownerId: "user-A");
        _repo.GetByIdAsync(prospect.Id, Arg.Any<CancellationToken>()).Returns(prospect);

        // Act: different user "attacker-B" tries to chat about user-A's prospect
        var act = () => _useCase.Handle(prospect.Id, "attacker-B", DefaultRequest());

        // Assert: the ownership gate must block them before any AI call
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Handle_WhenUserIsNotOwner_NeverCallsAiService()
    {
        // Critical: we must never invoke OpenAI if the user doesn't own the prospect
        var prospect = TestFactory.CreateValidManualProspect(ownerId: "user-A");
        _repo.GetByIdAsync(prospect.Id, Arg.Any<CancellationToken>()).Returns(prospect);

        try { await _useCase.Handle(prospect.Id, "attacker-B", DefaultRequest()); } catch { }

        await _chat.DidNotReceive().SendChatMessageAsync(
            Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<bool?>(), Arg.Any<double?>(), Arg.Any<int?>(),
            Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenUserIsOwner_InvokesAiAndReturnsResponse()
    {
        // Arrange
        var prospect = TestFactory.CreateValidManualProspect(ownerId: "user-A");
        _repo.GetByIdAsync(prospect.Id, Arg.Any<CancellationToken>()).Returns(prospect);

        var expectedResponse = new ChatResponseDto("Here is my answer", false, null, null, null);
        _chat.SendChatMessageAsync(
            Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<bool?>(), Arg.Any<double?>(), Arg.Any<int?>(),
            Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns((expectedResponse, "response-id-123"));

        // Act
        var result = await _useCase.Handle(prospect.Id, "user-A", DefaultRequest());

        // Assert
        result.Should().NotBeNull();
        result.AiMessage.Should().Be("Here is my answer");

        // Prospect should be updated with the new response ID
        await _repo.Received(1).UpdateAsync(prospect, Arg.Any<CancellationToken>());
    }
}
