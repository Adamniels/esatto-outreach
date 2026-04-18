using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Application.Features.Webhooks.Shared;
using Esatto.Outreach.Domain.Entities;
using Esatto.Outreach.Domain.Enums;
using Esatto.Outreach.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Esatto.Outreach.Application.Features.Webhooks.CreateOrUpdateProspectFromCapsule;

public class CreateOrUpdateProspectFromCapsuleCommandHandler
{
    private readonly IProspectRepository _prospectRepo;
    private readonly ILogger<CreateOrUpdateProspectFromCapsuleCommandHandler> _logger;

    public CreateOrUpdateProspectFromCapsuleCommandHandler(
        IProspectRepository prospectRepo,
        ILogger<CreateOrUpdateProspectFromCapsuleCommandHandler> logger)
    {
        _prospectRepo = prospectRepo;
        _logger = logger;
    }

    public async Task<WebhookResultDto> Handle(CreateOrUpdateProspectFromCapsuleCommand command, CancellationToken ct = default)
    {
        var party = command.Party;
        if (party.Id <= 0)
            return new WebhookResultDto(false, "Invalid party ID");
        if (string.IsNullOrWhiteSpace(party.Name))
            return new WebhookResultDto(false, "Party name is required");

        var existingProspect = await _prospectRepo.GetByExternalCrmIdAsync(CrmProvider.Capsule, party.Id.ToString(), ct);
        return existingProspect != null
            ? await UpdateExistingProspect(existingProspect, party, ct)
            : await CreateNewPendingProspect(party, ct);
    }

    private async Task<WebhookResultDto> UpdateExistingProspect(Prospect prospect, CapsulePartyDto party, CancellationToken ct)
    {
        var websites = MapWebsites(party.Websites);
        var tags = MapTags(party.Tags);
        var customFields = MapCustomFields(party.Fields);

        prospect.UpdateFromCrm(party.Name, party.About, party.UpdatedAt, party.LastContactedAt, party.PictureURL, websites, tags, customFields);
        await _prospectRepo.UpdateAsync(prospect, ct);
        _logger.LogInformation("Updated prospect: '{Name}' (Capsule ID: {CapsuleId})", party.Name, party.Id);
        return new WebhookResultDto(true, $"Updated prospect: {party.Name}");
    }

    private async Task<WebhookResultDto> CreateNewPendingProspect(CapsulePartyDto party, CancellationToken ct)
    {
        var websites = MapWebsites(party.Websites);
        var tags = MapTags(party.Tags);
        var customFields = MapCustomFields(party.Fields);

        var prospect = Prospect.CreatePendingFromCrm(
            CrmProvider.Capsule, party.Id.ToString(), party.Name!, party.About,
            party.CreatedAt ?? DateTime.UtcNow, party.UpdatedAt ?? DateTime.UtcNow,
            party.LastContactedAt, party.PictureURL, websites, tags, customFields);

        await _prospectRepo.AddAsync(prospect, ct);
        _logger.LogInformation("Created pending prospect: '{Name}' (Capsule ID: {CapsuleId})", party.Name, party.Id);
        return new WebhookResultDto(true, $"Created pending prospect: {party.Name}");
    }

    private static List<Website> MapWebsites(List<CapsuleWebsiteDto>? dtos) =>
        dtos == null || dtos.Count == 0 ? new List<Website>() : dtos.Select(w => new Website(w.Url, w.Service, w.Type)).ToList();

    private static List<Tag> MapTags(List<CapsuleTagDto>? dtos) =>
        dtos == null || dtos.Count == 0 ? new List<Tag>() : dtos.Select(t => new Tag(t.Id, t.Name, t.DataTag)).ToList();

    private static List<CustomField> MapCustomFields(List<CapsuleCustomFieldDto>? dtos) =>
        dtos == null || dtos.Count == 0 ? new List<CustomField>() : dtos.Select(f => new CustomField(f.Id, f.Definition?.Name, f.Definition?.Id, f.Value, f.TagId)).ToList();
}
