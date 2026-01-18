using Esatto.Outreach.Domain.Entities;
using System.Text.Json;
using Esatto.Outreach.Domain.Enums;
using Esatto.Outreach.Domain.ValueObjects;

namespace Esatto.Outreach.Application.DTOs;

// DTO för att skapa manuell prospect
public record ProspectCreateDto(
    string Name,
    List<string>? Websites,
    string? Notes);

// DTO för att uppdatera prospect (både manuell och Capsule)
public record ProspectUpdateDto(
    string? Name,
    List<string>? Websites,
    string? Notes,
    ProspectStatus? Status,
    string? MailTitle,
    string? MailBodyPlain,
    string? MailBodyHTML);

// DTO för att visa prospect (inklusive Capsule-data)
public record ProspectViewDto(
    Guid Id,
    string Name,
    bool IsFromCapsule,
    long? CapsuleId,
    bool IsPending,
    string? About,
    List<WebsiteDto> Websites,
    List<AddressDto> Addresses,
    List<TagDto> Tags,
    List<CustomFieldDto> CustomFields,
    string? PictureURL,
    DateTime? CapsuleCreatedAt,
    DateTime? CapsuleUpdatedAt,
    DateTime? LastContactedAt,
    string? Notes,
    ProspectStatus Status,
    DateTime CreatedUtc,
    DateTime? UpdatedUtc,
    string? MailTitle,
    string? MailBodyPlain,
    string? MailBodyHTML,
    string? OwnerId,
    EntityIntelligenceDto? EntityIntelligence,
    List<ContactPersonDto> ContactPersons)
{
    public static ProspectViewDto FromEntity(Prospect p) =>
        new(
            p.Id,
            p.Name,
            p.IsFromCapsule,
            p.CapsuleId,
            p.IsPending,
            p.About,
            p.Websites?.Select(w => new WebsiteDto(w.Url, w.Service, w.Type)).ToList() ?? new(),
            p.Addresses?.Select(a => new AddressDto(a.Street, a.City, a.State, a.Zip, a.Country, a.Type)).ToList() ?? new(),
            p.Tags?.Select(t => new TagDto(t.Id, t.Name, t.DataTag)).ToList() ?? new(),
            p.CustomFields?.Select(f => new CustomFieldDto(f.Id, f.FieldName, f.FieldDefinitionId, f.Value, f.TagId)).ToList() ?? new(),
            p.PictureURL,
            p.CapsuleCreatedAt,
            p.CapsuleUpdatedAt,
            p.LastContactedAt,
            p.Notes,
            p.Status,
            p.CreatedUtc,
            p.UpdatedUtc,
            p.MailTitle,
            p.MailBodyPlain,
            p.MailBodyHTML,
            p.OwnerId,
            p.EntityIntelligence != null ? EntityIntelligenceDto.FromEntity(p.EntityIntelligence) : null,
            p.ContactPersons?.Select(ContactPersonDto.FromEntity).ToList() ?? new());
}

// DTO för pending prospects lista
public record PendingProspectDto(
    Guid Id,
    string Name,
    long CapsuleId,
    string? About,
    string? PictureURL,
    List<WebsiteDto> Websites,
    DateTime CreatedUtc)
{
    public static PendingProspectDto FromEntity(Prospect p)
    {
        if (!p.IsPending)
            throw new InvalidOperationException("Cannot create PendingProspectDto from non-pending prospect");

        return new(
            p.Id,
            p.Name,
            p.CapsuleId!.Value,
            p.About,
            p.PictureURL,
            p.Websites?.Select(w => new WebsiteDto(w.Url, w.Service, w.Type)).ToList() ?? new(),
            p.CreatedUtc);
    }
}

// Nested DTOs för collections
public record WebsiteDto(string? Url, string? Service, string? Type);
public record EmailAddressDto(string? Address, string? Type);
public record PhoneNumberDto(string? Number, string? Type);
public record AddressDto(string? Street, string? City, string? State, string? Zip, string? Country, string? Type);
public record TagDto(long Id, string Name, bool DataTag);
public record CustomFieldDto(long Id, string? FieldName, long? FieldDefinitionId, string? Value, long? TagId);

// ...existing code...

public record EntityIntelligenceDto(
    Guid Id,
    Guid ProspectId,
    string? SummarizedContext,
    string? EnrichmentVersion,
    CompanyEnrichmentResult? EnrichedData,
    DateTime ResearchedAt,
    DateTime CreatedUtc,
    DateTime? UpdatedUtc,
    List<string> CompanyHooks
)
{
    public static EntityIntelligenceDto FromEntity(EntityIntelligence entity) 
    {
        return new(
            Id: entity.Id,
            ProspectId: entity.ProspectId,
            SummarizedContext: entity.SummarizedContext,
            EnrichmentVersion: entity.EnrichmentVersion,
            EnrichedData: entity.EnrichedData,
            ResearchedAt: entity.ResearchedAt,
            CreatedUtc: entity.CreatedUtc,
            UpdatedUtc: entity.UpdatedUtc,
            CompanyHooks: entity.EnrichedData?.OutreachHooks?.Select(h => h.HookDescription).ToList() ?? new()
        );
    }
}

// Rich Data Structures
// Legacy DTOs removed




