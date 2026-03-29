using Esatto.Outreach.Domain.Entities;
using Esatto.Outreach.Domain.Enums;
using Esatto.Outreach.Application.DTOs.Intelligence;

namespace Esatto.Outreach.Application.DTOs.Prospects;

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
    string? LinkedInMessage,
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
            p.LinkedInMessage,
            p.OwnerId,
            p.EntityIntelligence != null ? EntityIntelligenceDto.FromEntity(p.EntityIntelligence) : null,
            p.ContactPersons?.Select(ContactPersonDto.FromEntity).ToList() ?? new());
}
