using Esatto.Outreach.Domain.Entities;
using System.Text.Json;
using Esatto.Outreach.Domain.Enums;
using Esatto.Outreach.Domain.ValueObjects;

namespace Esatto.Outreach.Application.DTOs;

// DTO för att skapa manuell prospect
public record ProspectCreateDto(
    string Name,
    List<string>? Websites,
    List<string>? EmailAddresses,
    List<string>? PhoneNumbers,
    string? Notes);

// DTO för att uppdatera prospect (både manuell och Capsule)
public record ProspectUpdateDto(
    string? Name,
    List<string>? Websites,
    List<string>? EmailAddresses,
    List<string>? PhoneNumbers,
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
    List<EmailAddressDto> EmailAddresses,
    List<PhoneNumberDto> PhoneNumbers,
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
            p.EmailAddresses?.Select(e => new EmailAddressDto(e.Address, e.Type)).ToList() ?? new(),
            p.PhoneNumbers?.Select(ph => new PhoneNumberDto(ph.Number, ph.Type)).ToList() ?? new(),
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
    List<EmailAddressDto> EmailAddresses,
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
            p.EmailAddresses?.Select(e => new EmailAddressDto(e.Address, e.Type)).ToList() ?? new(),
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
    List<string> CompanyHooks,
    List<string> PersonalHooks,
    string? SummarizedContext,
    string? SourcesJson,
    EnrichedCompanyDataDto? RichData,
    DateTime ResearchedAt,
    DateTime CreatedUtc,
    DateTime? UpdatedUtc
)
{
    public static EntityIntelligenceDto FromEntity(EntityIntelligence entity) 
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        
        List<string> DeserializeList(string? json) 
        {
            if (string.IsNullOrWhiteSpace(json)) return new();
            try { return JsonSerializer.Deserialize<List<string>>(json, options) ?? new(); }
            catch { return new(); }
        }

        EnrichedCompanyDataDto? DeserializeRichData(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            try 
            { 
                 // If it starts with [, it's a list (legacy), so it's NOT rich data
                 if (json.TrimStart().StartsWith("[")) return null;
                 
                 return JsonSerializer.Deserialize<EnrichedCompanyDataDto>(json, options); 
            }
            catch { return null; }
        }

        return new(
            Id: entity.Id,
            ProspectId: entity.ProspectId,
            CompanyHooks: DeserializeList(entity.CompanyHooksJson), // Legacy/Fallback
            PersonalHooks: DeserializeList(entity.PersonalHooksJson),
            SummarizedContext: entity.SummarizedContext,
            SourcesJson: entity.SourcesJson,
            RichData: DeserializeRichData(entity.CompanyHooksJson), // Try new format
            ResearchedAt: entity.ResearchedAt,
            CreatedUtc: entity.CreatedUtc,
            UpdatedUtc: entity.UpdatedUtc
        );
    }
}

// Rich Data Structures
public record EnrichedCompanyDataDto(
    string Summary,
    List<string>? KeyValueProps,
    List<string>? TechStack,
    List<CaseStudyDto>? CaseStudies,
    List<NewsEventDto>? News,
    List<HiringSignalDto>? Hiring
);

public record CaseStudyDto(string Client, string Challenge, string Solution, string Outcome);
public record NewsEventDto(string Date, string Description, string Source);
public record HiringSignalDto(string Role, string Date, string Source);
