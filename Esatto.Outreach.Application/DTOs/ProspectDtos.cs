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
    string? MailBodyHTML)

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
            p.MailBodyHTML);
}
