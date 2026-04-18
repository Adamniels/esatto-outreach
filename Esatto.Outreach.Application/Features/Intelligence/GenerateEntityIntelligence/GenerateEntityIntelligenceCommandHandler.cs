using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Application.Abstractions.Services;
using Esatto.Outreach.Application.Features.Intelligence.Shared;
using Esatto.Outreach.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Esatto.Outreach.Application.Features.Intelligence.GenerateEntityIntelligence;

public sealed class GenerateEntityIntelligenceCommandHandler
{
    private readonly IEntityIntelligenceRepository _enrichmentRepo;
    private readonly IProspectRepository _prospectRepo;
    private readonly IContactDiscoveryProvider _contactDiscovery;
    private readonly ICompanyEnrichmentService _enrichmentService; // Injected
    private readonly ILogger<GenerateEntityIntelligenceCommandHandler> _logger;

    public GenerateEntityIntelligenceCommandHandler(
        IEntityIntelligenceRepository enrichmentRepo,
        IProspectRepository prospectRepo,
        IContactDiscoveryProvider contactDiscovery,
        ICompanyEnrichmentService enrichmentService,
        ILogger<GenerateEntityIntelligenceCommandHandler> logger)
    {
        _enrichmentRepo = enrichmentRepo;
        _prospectRepo = prospectRepo;
        _contactDiscovery = contactDiscovery;
        _enrichmentService = enrichmentService;
        _logger = logger;
    }

    public async Task<EntityIntelligenceDto> Handle(Guid prospectId, string userId, CancellationToken ct = default)
    {
        var prospect = await _prospectRepo.GetByIdReadOnlyAsync(prospectId, ct)
            ?? throw new KeyNotFoundException($"Prospect {prospectId} not found.");

        if (prospect.OwnerId != userId)
            throw new UnauthorizedAccessException("You don't have permission to access this prospect");

        _logger.LogInformation("Starting Entity Intelligence Enrichment for {Company}", prospect.Name);

        var companyDomain = prospect.GetPrimaryWebsite() ?? prospect.Name + ".com";
        var enrichmentResult = await _enrichmentService.EnrichCompanyAsync(prospect.Name, companyDomain, ct);
        var contacts = await _contactDiscovery.FindDecisionMakersAsync(prospect.Name, prospect.GetPrimaryWebsite() ?? "", ct);
        var trackedProspect = await _prospectRepo.GetByIdAsync(prospectId, ct)
             ?? throw new KeyNotFoundException($"Prospect {prospectId} not found.");
        var existingIntelligence = await _enrichmentRepo.GetByProspectIdAsync(prospectId, ct);

        if (contacts.Any())
        {
            foreach (var c in contacts)
            {
                _logger.LogInformation("Found detected contact: {Name} ({Title})", c.Name, c.Title);
                try
                {
                    var existingContact = trackedProspect.ContactPersons
                        .FirstOrDefault(cp => cp.Name.Equals(c.Name, StringComparison.OrdinalIgnoreCase));

                    if (existingContact == null)
                    {
                        var newContact = ContactPerson.Create(prospectId, c.Name, c.Title, null, c.LinkedInUrl);
                        await _prospectRepo.AddContactPersonAsync(newContact, ct);
                        _logger.LogInformation("Added new contact person: {Name}", c.Name);
                    }
                    else
                    {
                        _logger.LogDebug("Contact person {Name} already exists, skipping", c.Name);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to add contact {Name}, skipping", c.Name);
                }
            }
        }

        if (existingIntelligence is null)
        {
            existingIntelligence = EntityIntelligence.Create(
                prospectId,
                $"{enrichmentResult.Snapshot.WhatTheyDo} | {enrichmentResult.Snapshot.TargetCustomer}",
                enrichmentResult,
                "v2-company-strict"
            );
            await _enrichmentRepo.AddAsync(existingIntelligence, ct);
        }
        else
        {
            existingIntelligence.UpdateResearch(
                summarizedContext: $"{enrichmentResult.Snapshot.WhatTheyDo} | {enrichmentResult.Snapshot.TargetCustomer}",
                enrichedData: enrichmentResult,
                enrichmentVersion: "v2-company-strict"
            );
        }

        if (trackedProspect.EntityIntelligenceId != existingIntelligence.Id)
        {
            trackedProspect.LinkEntityIntelligence(existingIntelligence.Id);
        }

        await _prospectRepo.UpdateAsync(trackedProspect, ct);
        return EntityIntelligenceDto.FromEntity(existingIntelligence);
    }
}
