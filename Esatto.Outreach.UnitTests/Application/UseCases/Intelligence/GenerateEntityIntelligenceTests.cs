using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Application.Abstractions.Services;
using Esatto.Outreach.Application.UseCases.Intelligence;
using Esatto.Outreach.Domain.Entities;
using Esatto.Outreach.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Esatto.Outreach.UnitTests.Application.UseCases.Intelligence;

public class GenerateEntityIntelligenceTests
{
    private readonly IProspectRepository _prospectRepo;
    private readonly IEntityIntelligenceRepository _enrichmentRepo;
    private readonly IContactDiscoveryProvider _contactDiscovery;
    private readonly ICompanyEnrichmentService _enrichmentService;
    private readonly GenerateEntityIntelligence _useCase;

    public GenerateEntityIntelligenceTests()
    {
        _prospectRepo = Substitute.For<IProspectRepository>();
        _enrichmentRepo = Substitute.For<IEntityIntelligenceRepository>();
        _contactDiscovery = Substitute.For<IContactDiscoveryProvider>();
        _enrichmentService = Substitute.For<ICompanyEnrichmentService>();

        _useCase = new GenerateEntityIntelligence(
            _enrichmentRepo,
            _prospectRepo,
            _contactDiscovery,
            _enrichmentService,
            Substitute.For<ILogger<GenerateEntityIntelligence>>());
    }

    [Fact]
    public async Task Handle_WhenProspectNotFound_ThrowsKeyNotFoundException()
    {
        _prospectRepo.GetByIdReadOnlyAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Prospect?)null);

        var act = () => _useCase.Handle(Guid.NewGuid(), "any-user");

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenUserIsNotOwner_ThrowsUnauthorizedAccessException()
    {
        // Arrange: prospect owned by "user-A" found in DB
        var prospect = TestFactory.CreateValidManualProspect(ownerId: "user-A");
        _prospectRepo.GetByIdReadOnlyAsync(prospect.Id, Arg.Any<CancellationToken>()).Returns(prospect);

        // Act: attacker-B requests enrichment on user-A's prospect
        var act = () => _useCase.Handle(prospect.Id, "attacker-B");

        // Assert: must be rejected immediately
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Handle_WhenUserIsNotOwner_NeverCallsEnrichmentService()
    {
        // Critical: the expensive external enrichment calls must never fire for non-owners.
        // This would waste API credits and expose a company's prospect data to enrichment
        // pipelines initiated by a malicious user.
        var prospect = TestFactory.CreateValidManualProspect(ownerId: "user-A");
        _prospectRepo.GetByIdReadOnlyAsync(prospect.Id, Arg.Any<CancellationToken>()).Returns(prospect);

        try { await _useCase.Handle(prospect.Id, "attacker-B"); } catch { }

        await _enrichmentService.DidNotReceive()
            .EnrichCompanyAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());

        await _contactDiscovery.DidNotReceive()
            .FindDecisionMakersAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
