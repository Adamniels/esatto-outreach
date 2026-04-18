using Esatto.Outreach.Domain.Enums;

namespace Esatto.Outreach.Application.Features.Prospects;

public record ProspectUpdateDto(
    string? Name,
    List<string>? Websites,
    string? Notes,
    ProspectStatus? Status,
    string? MailTitle,
    string? MailBodyPlain,
    string? MailBodyHTML,
    string? LinkedInMessage);
