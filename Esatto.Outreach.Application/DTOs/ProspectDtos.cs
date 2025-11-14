using Esatto.Outreach.Domain.Entities;
using Esatto.Outreach.Domain.Enums;

namespace Esatto.Outreach.Application.DTOs;

public record ProspectCreateDto(
    string CompanyName,
    string? Domain,
    string? ContactName,
    string? ContactEmail,
    string? LinkedinUrl,
    string? Notes);

public record ProspectUpdateDto(
    string? CompanyName,
    string? Domain,
    string? ContactName,
    string? ContactEmail,
    string? LinkedinUrl,
    string? Notes,
    ProspectStatus? Status,
    string? MailTitle,
    string? MailBodyPlain,
    string? MailBodyHTML);

public record ProspectViewDto(
    Guid Id,
    string CompanyName,
    string? Domain,
    string? ContactName,
    string? ContactEmail,
    string? LinkedinUrl,
    string? Notes,
    ProspectStatus Status,
    DateTime CreatedUtc,
    DateTime? UpdatedUtc,
    string? MailTitle,
    string? MailBodyPlain,
    string? MailBodyHTML,
    SoftCompanyDataDto? SoftCompanyData)

{
    public static ProspectViewDto FromEntity(Prospect p) =>
        new(
            p.Id,
            p.CompanyName,
            p.Domain,
            p.ContactName,
            p.ContactEmail,
            p.LinkedinUrl,
            p.Notes,
            p.Status,
            p.CreatedUtc,
            p.UpdatedUtc,
            p.MailTitle,
            p.MailBodyPlain,
            p.MailBodyHTML,
            p.SoftCompanyData != null ? SoftCompanyDataDto.FromEntity(p.SoftCompanyData) : null);
}

// ...existing code...

public record SoftCompanyDataDto(
    Guid Id,
    Guid ProspectId,
    string? HooksJson,              // JSON string med hooks
    string? RecentEventsJson,       // JSON string med events
    string? NewsItemsJson,          // JSON string med news
    string? SocialActivityJson,     // JSON string med social media
    string? SourcesJson,            // JSON string med källor
    DateTime ResearchedAt,
    DateTime CreatedUtc,
    DateTime? UpdatedUtc
)
{
    public static SoftCompanyDataDto FromEntity(SoftCompanyData entity) => new(
        Id: entity.Id,
        ProspectId: entity.ProspectId,
        HooksJson: entity.HooksJson,
        RecentEventsJson: entity.RecentEventsJson,
        NewsItemsJson: entity.NewsItemsJson,
        SocialActivityJson: entity.SocialActivityJson,
        SourcesJson: entity.SourcesJson,
        ResearchedAt: entity.ResearchedAt,
        CreatedUtc: entity.CreatedUtc,
        UpdatedUtc: entity.UpdatedUtc
    );
}
