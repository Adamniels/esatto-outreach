using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Application.Features.Prospects.Shared;
using Esatto.Outreach.Application.Features.Prospects.UpdateProspect;
using Esatto.Outreach.Domain.Entities;
using Esatto.Outreach.UnitTests.Helpers;
using FluentAssertions;
using NSubstitute;

namespace Esatto.Outreach.UnitTests.Application.Features.Prospects.UpdateProspect;

public class UpdateProspectTests
{
    private readonly IProspectRepository _repo;
    private readonly UpdateProspectCommandHandler _useCase;

    public UpdateProspectTests()
    {
        _repo = Substitute.For<IProspectRepository>();
        _useCase = new UpdateProspectCommandHandler(_repo);
    }

    private static UpdateProspectRequest EmptyUpdate() => new(null, null, null, null, null, null, null, null);

    [Fact]
    public async Task Handle_WhenProspectNotFound_ReturnsNull()
    {
        _repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Prospect?)null);

        var result = await _useCase.Handle(Guid.NewGuid(), EmptyUpdate(), "any-user");

        ObjectAssertion.Should(result).BeNull();
    }

    [Fact]
    public async Task Handle_WhenUserIsNotOwner_ThrowsUnauthorizedAccessException()
    {
        // Arrange: prospect owned by "owner-A"
        var prospect = TestFactory.CreateValidManualProspect(ownerId: "owner-A");
        _repo.GetByIdAsync(prospect.Id, Arg.Any<CancellationToken>()).Returns(prospect);

        // Act: different user "attacker-B" tries to update it
        var act = () => _useCase.Handle(prospect.Id, EmptyUpdate(), userId: "attacker-B");

        // Assert: must be rejected — this is the core ownership invariant
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Handle_WhenUserIsOwner_UpdatesAndPersists()
    {
        // Arrange
        var prospect = TestFactory.CreateValidManualProspect(ownerId: "owner-A");
        _repo.GetByIdAsync(prospect.Id, Arg.Any<CancellationToken>()).Returns(prospect);
        var request = new UpdateProspectRequest("Updated Name", null, null, null, null, null, null, null);

        // Act
        var result = await _useCase.Handle(prospect.Id, request, userId: "owner-A");

        // Assert
        ObjectAssertion.Should(result).NotBeNull();
        await _repo.Received(1).UpdateAsync(prospect, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenUserIsNotOwner_NeverCallsUpdateAsync()
    {
        // This verifies that the authorization check happens BEFORE any write
        var prospect = TestFactory.CreateValidManualProspect(ownerId: "owner-A");
        _repo.GetByIdAsync(prospect.Id, Arg.Any<CancellationToken>()).Returns(prospect);

        try { await _useCase.Handle(prospect.Id, EmptyUpdate(), "attacker-B"); } catch { }

        await _repo.DidNotReceive().UpdateAsync(Arg.Any<Prospect>(), Arg.Any<CancellationToken>());
    }
}
