using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Application.DTOs.Webhooks;
using Esatto.Outreach.Domain.Entities;
using Esatto.Outreach.Domain.Enums;
using Esatto.Outreach.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Esatto.Outreach.Application.UseCases.Webhooks;

/// <summary>
/// Handles party/created and party/updated webhooks from Capsule CRM.
/// Creates new pending prospect or updates existing one.
/// Maps Capsule-specific DTOs to generic domain value objects.
/// </summary>
public class CreateOrUpdateProspectFromCapsule
{
    private readonly IProspectRepository _prospectRepo;
    private readonly ILogger<CreateOrUpdateProspectFromCapsule> _logger;

    public CreateOrUpdateProspectFromCapsule(
        IProspectRepository prospectRepo,
        ILogger<CreateOrUpdateProspectFromCapsule> logger)
    {
        _prospectRepo = prospectRepo;
        _logger = logger;
    }

    public async Task<WebhookResultDto> Handle(
        CapsulePartyDto party,
        CancellationToken ct = default)
    {
        if (party.Id <= 0)
        {
            _logger.LogWarning("Invalid party ID: {Id}", party.Id);
            return new WebhookResultDto(false, "Invalid party ID");
        }

        if (string.IsNullOrWhiteSpace(party.Name))
        {
            _logger.LogWarning("Party missing name (ID: {Id})", party.Id);
            return new WebhookResultDto(false, "Party name is required");
        }

        // Check if prospect already exists (via external CRM ID)
        var existingProspect = await _prospectRepo.GetByExternalCrmIdAsync(
            CrmProvider.Capsule, 
            party.Id.ToString(), 
            ct);

        if (existingProspect != null)
        {
            // Update existing prospect (even if pending)
            return await UpdateExistingProspect(existingProspect, party, ct);
        }
        else
        {
            // Create new pending prospect
            return await CreateNewPendingProspect(party, ct);
        }
    }

    private async Task<WebhookResultDto> UpdateExistingProspect(
        Prospect prospect,
        CapsulePartyDto party,
        CancellationToken ct)
    {
        _logger.LogInformation(
            "Updating prospect from Capsule: '{Name}' (ID: {Id}, IsPending: {IsPending})",
            party.Name,
            party.Id,
            prospect.IsPending);

        // Map Capsule DTOs to generic domain value objects
        var websites = MapWebsites(party.Websites);
        var tags = MapTags(party.Tags);
        var customFields = MapCustomFields(party.Fields);

        prospect.UpdateFromCrm(
            name: party.Name,
            about: party.About,
            crmUpdatedAt: party.UpdatedAt,
            lastContactedAt: party.LastContactedAt,
            pictureURL: party.PictureURL,
            websites: websites,
            tags: tags,
            customFields: customFields
        );

        await _prospectRepo.UpdateAsync(prospect, ct);

        _logger.LogInformation(
            "Updated prospect: '{Name}' (Capsule ID: {CapsuleId})",
            party.Name,
            party.Id);

        return new WebhookResultDto(true, $"Updated prospect: {party.Name}");
    }

    private async Task<WebhookResultDto> CreateNewPendingProspect(
        CapsulePartyDto party,
        CancellationToken ct)
    {
        _logger.LogInformation(
            "Creating new pending prospect from Capsule: '{Name}' (ID: {Id})",
            party.Name,
            party.Id);

        // Map Capsule DTOs to generic domain value objects
        var websites = MapWebsites(party.Websites);
        var tags = MapTags(party.Tags);
        var customFields = MapCustomFields(party.Fields);

        var prospect = Prospect.CreatePendingFromCrm(
            crmSource: CrmProvider.Capsule,
            externalCrmId: party.Id.ToString(),
            name: party.Name!,
            about: party.About,
            crmCreatedAt: party.CreatedAt ?? DateTime.UtcNow,
            crmUpdatedAt: party.UpdatedAt ?? DateTime.UtcNow,
            lastContactedAt: party.LastContactedAt,
            pictureURL: party.PictureURL,
            websites: websites,
            tags: tags,
            customFields: customFields
        );

        await _prospectRepo.AddAsync(prospect, ct);

        _logger.LogInformation(
            "Created pending prospect: '{Name}' (Capsule ID: {CapsuleId})",
            party.Name,
            party.Id);

        return new WebhookResultDto(true, $"Created pending prospect: {party.Name}");
    }

    // Mapping helpers: Convert Capsule DTOs to generic domain value objects
    private static List<Website> MapWebsites(List<CapsuleWebsiteDto>? dtos)
    {
        if (dtos == null || dtos.Count == 0)
            return new List<Website>();

        return dtos.Select(w => new Website(
            w.Url,
            w.Service,
            w.Type
        )).ToList();
    }

    private static List<Tag> MapTags(List<CapsuleTagDto>? dtos)
    {
        if (dtos == null || dtos.Count == 0)
            return new List<Tag>();

        return dtos.Select(t => new Tag(
            t.Id,
            t.Name,
            t.DataTag
        )).ToList();
    }

    private static List<CustomField> MapCustomFields(List<CapsuleCustomFieldDto>? dtos)
    {
        if (dtos == null || dtos.Count == 0)
            return new List<CustomField>();

        return dtos.Select(f => new CustomField(
            f.Id,
            f.Definition?.Name,
            f.Definition?.Id,
            f.Value,
            f.TagId
        )).ToList();
    }
}
