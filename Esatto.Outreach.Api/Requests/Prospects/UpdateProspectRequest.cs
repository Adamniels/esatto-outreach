using Esatto.Outreach.Domain.Enums;

namespace Esatto.Outreach.Api.Requests.Prospects;

public sealed record UpdateProspectRequest(
    string? Name,
    List<string>? Websites,
    string? Notes,
    ProspectStatus? Status,
    string? MailTitle,
    string? MailBodyPlain,
    string? MailBodyHtml,
    string? LinkedInMessage
);
