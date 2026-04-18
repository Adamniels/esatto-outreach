using Esatto.Outreach.Domain.Enums;

namespace Esatto.Outreach.Application.Features.Prospects.UpdateProspect;

public record UpdateProspectRequest(
    string? Name,
    List<string>? Websites,
    string? Notes,
    ProspectStatus? Status,
    string? MailTitle,
    string? MailBodyPlain,
    string? MailBodyHtml,
    string? LinkedInMessage);
