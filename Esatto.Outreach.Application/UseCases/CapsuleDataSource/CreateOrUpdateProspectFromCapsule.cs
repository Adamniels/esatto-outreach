using Esatto.Outreach.Application.Abstractions;
using Esatto.Outreach.Application.DTOs;
using Esatto.Outreach.Domain.Entities;
using Esatto.Outreach.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Esatto.Outreach.Application.UseCases.CapsuleDataSource;

/// <summary>
/// Hanterar party/created och party/updated webhooks från Capsule CRM.
/// Skapar ny pending prospect eller uppdaterar befintlig.
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

        // Kolla om prospect redan finns (via CapsuleId)
        var existingProspect = await _prospectRepo.GetByCapsuleIdAsync(party.Id, ct);

        if (existingProspect != null)
        {
            // Uppdatera befintlig prospect (även om den är pending)
            return await UpdateExistingProspect(existingProspect, party, ct);
        }
        else
        {
            // Skapa ny pending prospect
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

        // Mappa nested data
        var addresses = MapAddresses(party.Addresses);
        var websites = MapWebsites(party.Websites);
        var tags = MapTags(party.Tags);
        var customFields = MapCustomFields(party.Fields);

        prospect.UpdateFromCapsule(
            name: party.Name,
            about: party.About,
            capsuleUpdatedAt: party.UpdatedAt,
            lastContactedAt: party.LastContactedAt,
            pictureURL: party.PictureURL,
            addresses: addresses,
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

        // Mappa nested data
        var addresses = MapAddresses(party.Addresses);
        var websites = MapWebsites(party.Websites);
        var tags = MapTags(party.Tags);
        var customFields = MapCustomFields(party.Fields);

        var prospect = Prospect.CreatePendingFromCapsule(
            capsuleId: party.Id,
            name: party.Name,
            about: party.About,
            capsuleCreatedAt: party.CreatedAt ?? DateTime.UtcNow,
            capsuleUpdatedAt: party.UpdatedAt ?? DateTime.UtcNow,
            lastContactedAt: party.LastContactedAt,
            pictureURL: party.PictureURL,
            addresses: addresses,
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

    // Mapping helpers
    private static List<CapsuleAddress> MapAddresses(List<CapsuleAddressDto>? dtos)
    {
        if (dtos == null || dtos.Count == 0)
            return new List<CapsuleAddress>();

        return dtos.Select(a => new CapsuleAddress(
            a.Street,
            a.City,
            a.State,
            a.Zip,
            a.Country,
            a.Type
        )).ToList();
    }

    private static List<CapsulePhoneNumber> MapPhoneNumbers(List<CapsulePhoneDto>? dtos)
    {
        if (dtos == null || dtos.Count == 0)
            return new List<CapsulePhoneNumber>();

        return dtos.Select(p => new CapsulePhoneNumber(
            p.Number,
            p.Type
        )).ToList();
    }

    private static List<CapsuleEmailAddress> MapEmailAddresses(List<CapsuleEmailDto>? dtos)
    {
        if (dtos == null || dtos.Count == 0)
            return new List<CapsuleEmailAddress>();

        return dtos.Select(e => new CapsuleEmailAddress(
            e.Address,
            e.Type
        )).ToList();
    }

    private static List<CapsuleWebsite> MapWebsites(List<CapsuleWebsiteDto>? dtos)
    {
        if (dtos == null || dtos.Count == 0)
            return new List<CapsuleWebsite>();

        return dtos.Select(w => new CapsuleWebsite(
            w.Url,
            w.Service,
            w.Type
        )).ToList();
    }

    private static List<CapsuleTag> MapTags(List<CapsuleTagDto>? dtos)
    {
        if (dtos == null || dtos.Count == 0)
            return new List<CapsuleTag>();

        return dtos.Select(t => new CapsuleTag(
            t.Id,
            t.Name,
            t.DataTag
        )).ToList();
    }

    private static List<CapsuleCustomField> MapCustomFields(List<CapsuleCustomFieldDto>? dtos)
    {
        if (dtos == null || dtos.Count == 0)
            return new List<CapsuleCustomField>();

        return dtos.Select(f => new CapsuleCustomField(
            f.Id,
            f.Definition?.Name,
            f.Definition?.Id,
            f.Value,
            f.TagId
        )).ToList();
    }
}
