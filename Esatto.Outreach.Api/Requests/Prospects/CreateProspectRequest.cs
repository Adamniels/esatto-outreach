namespace Esatto.Outreach.Api.Requests.Prospects;

public sealed record CreateProspectRequest(
    string Name,
    List<string>? Websites,
    string? Notes
);
