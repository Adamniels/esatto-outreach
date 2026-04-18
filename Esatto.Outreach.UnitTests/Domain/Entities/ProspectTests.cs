using FluentAssertions;
using Esatto.Outreach.Domain.Entities;
using Esatto.Outreach.Domain.Enums;
using Esatto.Outreach.UnitTests.Helpers;

namespace Esatto.Outreach.UnitTests.Domain.Entities;

public class ProspectTests
{
    [Fact]
    public void CreateManual_WithValidInput_ReturnsActiveProspect()
    {
        // Arrange
        var name = "Manual Test Company";
        var ownerId = "owner-123";

        // Act
        var prospect = Prospect.CreateManual(name, ownerId);

        // Assert
        ObjectAssertion.Should(prospect).NotBeNull();
        prospect.Name.Should().Be(name);
        prospect.OwnerId.Should().Be(ownerId);
        prospect.IsPending.Should().BeFalse();
        prospect.Status.Should().Be(ProspectStatus.New);
        prospect.CrmSource.Should().Be(CrmProvider.None);
        prospect.IsFromCrm.Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void CreateManual_WithInvalidName_ThrowsArgumentException(string? invalidName)
    {
        // Arrange
        var ownerId = "owner-123";

        // Act & Assert
        Action act = () => Prospect.CreateManual(invalidName!, ownerId);
        act.Should().Throw<ArgumentException>().WithParameterName("name");
    }

    [Fact]
    public void CreatePendingFromCrm_WithValidInput_ReturnsPendingProspect()
    {
        // Arrange
        var crmId = "ext-456";
        var name = "CRM Test Entity";

        // Act
        var prospect = Prospect.CreatePendingFromCrm(
            CrmProvider.Capsule,
            crmId,
            name,
            null,
            DateTime.UtcNow,
            DateTime.UtcNow,
            null,
            null);

        // Assert
        ObjectAssertion.Should(prospect).NotBeNull();
        prospect.Name.Should().Be(name);
        prospect.ExternalCrmId.Should().Be(crmId);
        prospect.CrmSource.Should().Be(CrmProvider.Capsule);
        prospect.IsFromCrm.Should().BeTrue();
        prospect.IsPending.Should().BeTrue();
        prospect.OwnerId.Should().BeNull();
    }

    [Fact]
    public void Claim_OnPendingCrmProspect_AssignsOwnerAndRemovesPendingStatus()
    {
        // Arrange
        var prospect = TestFactory.CreateValidPendingCrmProspect();
        var newOwnerId = "owner-999";

        // Act
        prospect.Claim(newOwnerId);

        // Assert
        prospect.IsPending.Should().BeFalse();
        prospect.OwnerId.Should().Be(newOwnerId);
    }

    [Fact]
    public void Claim_OnAlreadyClaimedProspect_ThrowsInvalidOperationException()
    {
        // Arrange
        var prospect = TestFactory.CreateValidManualProspect(); // Already not pending
        
        // Act & Assert
        Action act = () => prospect.Claim("new-owner");
        act.Should().Throw<InvalidOperationException>().WithMessage("*Cannot claim prospect that is not pending*");
    }

    [Fact]
    public void SetActiveContact_WithValidContact_ActivatesExpectedContactAndDeactivatesOthers()
    {
        // Arrange
        var prospect = TestFactory.CreateValidManualProspect();
        
        // Add multiple contacts
        prospect.AddContactPersonCommandHandler("Person A", "CEO", "a@test.com");
        prospect.AddContactPersonCommandHandler("Person B", "CTO", "b@test.com");
        prospect.AddContactPersonCommandHandler("Person C", "CFO", "c@test.com");

        var contactA = prospect.ContactPersons.First(c => c.Name == "Person A");
        var contactB = prospect.ContactPersons.First(c => c.Name == "Person B");
        var contactC = prospect.ContactPersons.First(c => c.Name == "Person C");

        // Act - activate B
        prospect.SetActiveContactCommandHandler(contactB.Id);

        // Assert
        ObjectAssertion.Should(prospect.GetActiveContactQueryHandler()).Be(contactB);
        contactB.IsActive.Should().BeTrue();

        // Act - switch to C
        prospect.SetActiveContactCommandHandler(contactC.Id);

        // Assert
        ObjectAssertion.Should(prospect.GetActiveContactQueryHandler()).Be(contactC);
        contactB.IsActive.Should().BeFalse();
        contactC.IsActive.Should().BeTrue();
        contactA.IsActive.Should().BeFalse();
    }
}
