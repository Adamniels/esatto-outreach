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
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GenerateEntityIntelligenceCommandHandler> _logger;

    public GenerateEntityIntelligenceCommandHandler(
        IEntityIntelligenceRepository enrichmentRepo,
        IProspectRepository prospectRepo,
        IContactDiscoveryProvider contactDiscovery,
        ICompanyEnrichmentService enrichmentService,
        IUnitOfWork unitOfWork,
        ILogger<GenerateEntityIntelligenceCommandHandler> logger)
    {
        _enrichmentRepo = enrichmentRepo;
        _prospectRepo = prospectRepo;
        _contactDiscovery = contactDiscovery;
        _enrichmentService = enrichmentService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<EntityIntelligenceDto> Handle(GenerateEntityIntelligenceCommand command, string userId, CancellationToken ct = default)
    {
        var prospect = await _prospectRepo.GetByIdReadOnlyAsync(command.ProspectId, ct)
            ?? throw new KeyNotFoundException($"Prospect {command.ProspectId} not found.");

        if (prospect.OwnerId != userId)
            throw new UnauthorizedAccessException("You don't have permission to access this prospect");

        _logger.LogInformation("Starting Entity Intelligence Enrichment for {Company}", prospect.Name);

        var companyDomain = prospect.GetPrimaryWebsite() ?? prospect.Name + ".com";
        var enrichmentResult = await _enrichmentService.EnrichCompanyAsync(prospect.Name, companyDomain, ct);
        var contacts = await _contactDiscovery.FindDecisionMakersAsync(prospect.Name, prospect.GetPrimaryWebsite() ?? "", ct);
        var trackedProspect = await _prospectRepo.GetByIdAsync(command.ProspectId, ct)
             ?? throw new KeyNotFoundException($"Prospect {command.ProspectId} not found.");
        var existingIntelligence = await _enrichmentRepo.GetByProspectIdAsync(command.ProspectId, ct);

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
                        var newContact = ContactPerson.Create(command.ProspectId, c.Name, c.Title, null, c.LinkedInUrl);
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
                command.ProspectId,
                $"{enrichmentResult.Snapshot.WhatTheyDo} | {enrichmentResult.Snapshot.TargetCustomer}",
                enrichmentResult,
                "v3-openai-websearch-json"
            );
            await _enrichmentRepo.AddAsync(existingIntelligence, ct);
        }
        else
        {
            existingIntelligence.UpdateResearch(
                summarizedContext: $"{enrichmentResult.Snapshot.WhatTheyDo} | {enrichmentResult.Snapshot.TargetCustomer}",
                enrichedData: enrichmentResult,
                enrichmentVersion: "v3-openai-websearch-json"
            );
        }

        if (trackedProspect.EntityIntelligenceId != existingIntelligence.Id)
        {
            trackedProspect.LinkEntityIntelligence(existingIntelligence.Id);
        }

        await _prospectRepo.UpdateAsync(trackedProspect, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return EntityIntelligenceDto.FromEntity(existingIntelligence);
    }
}
