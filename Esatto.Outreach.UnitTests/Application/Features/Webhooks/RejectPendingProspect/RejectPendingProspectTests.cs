using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Application.Features.Webhooks.RejectPendingProspect;
using Esatto.Outreach.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using System.Reflection;

namespace Esatto.Outreach.UnitTests.Application.Features.Webhooks.RejectPendingProspect;

public class RejectPendingProspectTests
{
    [Fact]
    public async Task Handle_WhenPendingProspectOwnedByAnotherUser_ThrowsUnauthorized()
    {
        var repo = Substitute.For<IProspectRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var logger = Substitute.For<ILogger<RejectPendingProspectCommandHandler>>();
        var handler = new RejectPendingProspectCommandHandler(repo, unitOfWork, logger);

        var prospect = TestFactory.CreateValidPendingCrmProspect();
        typeof(global::Esatto.Outreach.Domain.Entities.Prospect)
            .GetProperty("OwnerId", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!
            .SetValue(prospect, "owner-A");
        repo.GetByIdAsync(prospect.Id, Arg.Any<CancellationToken>()).Returns(prospect);

        var act = () => handler.Handle(new RejectPendingProspectCommand(prospect.Id), "attacker-B");

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
        await repo.DidNotReceive().DeleteAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }
}
