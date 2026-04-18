using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Application.Features.Intelligence.ResetProspectChat;
using Esatto.Outreach.UnitTests.Helpers;
using FluentAssertions;
using NSubstitute;

namespace Esatto.Outreach.UnitTests.Application.Features.Intelligence.ResetProspectChat;

public class ResetProspectChatTests
{
    [Fact]
    public async Task Handle_WhenUserIsNotOwner_ThrowsUnauthorized()
    {
        var repo = Substitute.For<IProspectRepository>();
        var handler = new ResetProspectChatCommandHandler(repo, Substitute.For<IUnitOfWork>());
        var prospect = TestFactory.CreateValidManualProspect(ownerId: "owner-A");
        repo.GetByIdAsync(prospect.Id, Arg.Any<CancellationToken>()).Returns(prospect);

        var act = () => handler.Handle(new ResetProspectChatCommand(prospect.Id), "attacker-B");

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
        await repo.DidNotReceive().UpdateAsync(Arg.Any<global::Esatto.Outreach.Domain.Entities.Prospect>(), Arg.Any<CancellationToken>());
    }
}
