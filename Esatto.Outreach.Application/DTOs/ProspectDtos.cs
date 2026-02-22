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

// DTO for viewing prospect (CRM-agnostic)
public record ProspectViewDto(
    Guid Id,
    string Name,
    CrmProvider CrmSource,
    string? ExternalCrmId,
    bool IsPending,
    string? About,
    List<WebsiteDto> Websites,
    List<TagDto> Tags,
    List<CustomFieldDto> CustomFields,
    string? PictureURL,
    
    // CRM timestamps
    DateTime? CrmCreatedAt,
    DateTime? CrmUpdatedAt,
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
            p.CrmSource,
            p.ExternalCrmId,
            p.IsPending,
            p.About,
            p.Websites?.Select(w => new WebsiteDto(w.Url, w.Service, w.Type)).ToList() ?? new(),
            p.Tags?.Select(t => new TagDto(t.Id, t.Name, t.DataTag)).ToList() ?? new(),
            p.CustomFields?.Select(f => new CustomFieldDto(f.Id, f.FieldName, f.FieldDefinitionId, f.Value, f.TagId)).ToList() ?? new(),
            p.PictureURL,
            p.CrmCreatedAt,
            p.CrmUpdatedAt,
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

// DTO for pending prospects list
public record PendingProspectDto(
    Guid Id,
    string Name,
    CrmProvider CrmSource,
    string? ExternalCrmId,
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
            p.CrmSource,
            p.ExternalCrmId,
            p.About,
            p.PictureURL,
            p.Websites?.Select(w => new WebsiteDto(w.Url, w.Service, w.Type)).ToList() ?? new(),
            p.CreatedUtc);
    }
}

// Nested DTOs for collections (CRM-agnostic)
public record WebsiteDto(string? Url, string? Service, string? Type);
public record TagDto(long? Id, string Name, bool DataTag);  // Id nullable for manual tags
public record CustomFieldDto(long? Id, string? FieldName, long? FieldDefinitionId, string? Value, long? TagId);  // Id nullable for manual fields

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




