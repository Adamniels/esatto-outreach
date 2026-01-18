using Esatto.Outreach.Application.Abstractions;
using Esatto.Outreach.Application.DTOs;
using Esatto.Outreach.Domain.Entities;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Esatto.Outreach.Application.UseCases.SoftDataCollection;

public sealed class GenerateEntityIntelligence
{
    private readonly IEntityIntelligenceRepository _enrichmentRepo;
    private readonly IProspectRepository _prospectRepo;
    private readonly IContactDiscoveryProvider _contactDiscovery;
    private readonly ICompanyEnrichmentService _enrichmentService; // Injected
    private readonly ILogger<GenerateEntityIntelligence> _logger;

    public GenerateEntityIntelligence(
        IEntityIntelligenceRepository enrichmentRepo,
        IProspectRepository prospectRepo,
        IContactDiscoveryProvider contactDiscovery,
        ICompanyEnrichmentService enrichmentService,
        ILogger<GenerateEntityIntelligence> logger)
    {
        _enrichmentRepo = enrichmentRepo;
        _prospectRepo = prospectRepo;
        _contactDiscovery = contactDiscovery;
        _enrichmentService = enrichmentService;
        _logger = logger;
    }

    public async Task<EntityIntelligenceDto> Handle(Guid prospectId, CancellationToken ct = default)
    {
        // 1. Fetch Prospect (ReadOnly for scraping context)
        var prospect = await _prospectRepo.GetByIdReadOnlyAsync(prospectId, ct)
            ?? throw new KeyNotFoundException($"Prospect {prospectId} not found.");

        _logger.LogInformation("Starting Entity Intelligence Enrichment for {Company}", prospect.Name);

        // 2. Perform Strict Company Enrichment (New Service)
        var companyDomain = prospect.GetPrimaryWebsite() ?? prospect.Name + ".com";
        var enrichmentResult = await _enrichmentService.EnrichCompanyAsync(prospect.Name, companyDomain, ct);

        // 3. Contact Discovery (Use ReadOnly prospect data)
        // Perform this BEFORE loading the tracked entity to avoid stale data during long API calls.
        var contacts = await _contactDiscovery.FindDecisionMakersAsync(prospect.Name, prospect.GetPrimaryWebsite() ?? "", ct);

        // 4. LOAD TRACKED ENTITIES & TRANSACTION START
        // Now that all external/slow operations are done, load the fresh entities for update.
        var trackedProspect = await _prospectRepo.GetByIdAsync(prospectId, ct)
             ?? throw new KeyNotFoundException($"Prospect {prospectId} not found.");

        var existingIntelligence = await _enrichmentRepo.GetByProspectIdAsync(prospectId, ct);

        // 5. Apply Contact Updates
        if (contacts.Any())
        {
            foreach (var c in contacts)
            {
                _logger.LogInformation("Found detected contact: {Name} ({Title})", c.Name, c.Title);

                try
                {
                    // Check if contact already exists
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

        // 6. Apply Entity Intelligence Updates
        if (existingIntelligence is null)
        {
            existingIntelligence = EntityIntelligence.Create(
                prospectId,
                $"{enrichmentResult.Snapshot.WhatTheyDo} | {enrichmentResult.Snapshot.TargetCustomer}",
                enrichmentResult,
                "v2-company-strict"
            );
            // Ensure it's added to context
            await _enrichmentRepo.AddAsync(existingIntelligence, ct);
        }
        else
        {
            existingIntelligence.UpdateResearch(
                summarizedContext: $"{enrichmentResult.Snapshot.WhatTheyDo} | {enrichmentResult.Snapshot.TargetCustomer}",
                enrichedData: enrichmentResult,
                enrichmentVersion: "v2-company-strict"
            );
            // No need to call UpdateAsync explicitly if we save trackedProspect below, 
            // provided the context is shared. But for safety/clarity we rely on EF tracking.
        }

        // 7. Link & Save (Single Transaction)
        // Linking by ID ensures the relationship is set
        if (trackedProspect.EntityIntelligenceId != existingIntelligence.Id)
        {
            trackedProspect.LinkEntityIntelligence(existingIntelligence.Id);
        }

        // Save the aggregate root (Prospect). This commits all changes to Prospect, Contacts, and Intelligence.
        await _prospectRepo.UpdateAsync(trackedProspect, ct);

        return EntityIntelligenceDto.FromEntity(existingIntelligence);
    }
}
