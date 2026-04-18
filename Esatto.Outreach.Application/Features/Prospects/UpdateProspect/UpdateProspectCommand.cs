using Esatto.Outreach.Domain.Enums;

namespace Esatto.Outreach.Application.Features.Prospects.UpdateProspect;

public sealed record UpdateProspectCommand(
    Guid Id,
    string? Name,
    List<string>? Websites,
    string? Notes,
    ProspectStatus? Status,
    string? MailTitle,
    string? MailBodyPlain,
    string? MailBodyHtml,
    string? LinkedInMessage
);
