using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Application.Features.Prospects.GetProspectById;
using Esatto.Outreach.UnitTests.Helpers;
using FluentAssertions;
using NSubstitute;

namespace Esatto.Outreach.UnitTests.Application.Features.Prospects.GetProspectById;

public class GetProspectByIdTests
{
    private readonly IProspectRepository _repo;
    private readonly GetProspectByIdQueryHandler _useCase;

    public GetProspectByIdTests()
    {
        _repo = Substitute.For<IProspectRepository>();
        _useCase = new GetProspectByIdQueryHandler(_repo);
    }

    [Fact]
    public async Task Handle_WhenProspectNotFound_ReturnsNull()
    {
        _repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((global::Esatto.Outreach.Domain.Entities.Prospect?)null);

        var result = await _useCase.Handle(new GetProspectByIdQuery(Guid.NewGuid()), "owner-A");

        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WhenUserIsNotOwner_ThrowsUnauthorizedAccessException()
    {
        var prospect = TestFactory.CreateValidManualProspect(ownerId: "owner-A");
        _repo.GetByIdAsync(prospect.Id, Arg.Any<CancellationToken>()).Returns(prospect);

        var act = () => _useCase.Handle(new GetProspectByIdQuery(prospect.Id), "attacker-B");

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Handle_WhenUserIsOwner_ReturnsProspect()
    {
        var prospect = TestFactory.CreateValidManualProspect(ownerId: "owner-A");
        _repo.GetByIdAsync(prospect.Id, Arg.Any<CancellationToken>()).Returns(prospect);

        var result = await _useCase.Handle(new GetProspectByIdQuery(prospect.Id), "owner-A");

        result.Should().NotBeNull();
        result!.Id.Should().Be(prospect.Id);
    }
}
