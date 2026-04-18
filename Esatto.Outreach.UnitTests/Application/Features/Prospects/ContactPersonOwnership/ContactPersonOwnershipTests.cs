using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Application.Features.Prospects.AddContactPerson;
using Esatto.Outreach.Application.Features.Prospects.DeleteContactPerson;
using Esatto.Outreach.Application.Features.Prospects.UpdateContactPerson;
using Esatto.Outreach.Domain.Entities;
using Esatto.Outreach.UnitTests.Helpers;
using FluentAssertions;
using NSubstitute;

namespace Esatto.Outreach.UnitTests.Application.Features.Prospects.ContactPersonOwnership;

public class ContactPersonOwnershipTests
{
    private readonly IProspectRepository _repo = Substitute.For<IProspectRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    [Fact]
    public async Task AddContact_WhenUserIsNotOwner_ThrowsUnauthorized()
    {
        var prospect = TestFactory.CreateValidManualProspect(ownerId: "owner-A");
        _repo.GetByIdAsync(prospect.Id, Arg.Any<CancellationToken>()).Returns(prospect);
        var handler = new AddContactPersonCommandHandler(_repo, _unitOfWork);

        var act = () => handler.Handle(new AddContactPersonCommand(prospect.Id, "Jane", null, null, null), "attacker-B");

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
        await _repo.DidNotReceive().AddContactPersonAsync(Arg.Any<ContactPerson>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateContact_WhenUserIsNotOwner_ThrowsUnauthorized()
    {
        var prospect = TestFactory.CreateValidManualProspect(ownerId: "owner-A");
        prospect.AddContactPerson("Jane");
        var contact = prospect.ContactPersons.Single();

        _repo.GetByIdAsync(prospect.Id, Arg.Any<CancellationToken>()).Returns(prospect);
        var handler = new UpdateContactPersonCommandHandler(_repo, _unitOfWork);
        var command = new UpdateContactPersonCommand(prospect.Id, contact.Id, "Jane 2", null, null, null, null, null, null);

        var act = () => handler.Handle(command, "attacker-B");

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
        await _repo.DidNotReceive().UpdateContactPersonAsync(Arg.Any<ContactPerson>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteContact_WhenUserIsNotOwner_ThrowsUnauthorized()
    {
        var prospect = TestFactory.CreateValidManualProspect(ownerId: "owner-A");
        prospect.AddContactPerson("Jane");
        var contact = prospect.ContactPersons.Single();

        _repo.GetByIdAsync(prospect.Id, Arg.Any<CancellationToken>()).Returns(prospect);
        var handler = new DeleteContactPersonCommandHandler(_repo, _unitOfWork);

        var act = () => handler.Handle(new DeleteContactPersonCommand(prospect.Id, contact.Id), "attacker-B");

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
        await _repo.DidNotReceive().DeleteContactPersonAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }
}
