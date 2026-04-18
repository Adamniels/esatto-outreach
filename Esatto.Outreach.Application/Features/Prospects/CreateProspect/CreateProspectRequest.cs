namespace Esatto.Outreach.Application.Features.Prospects.CreateProspect;

public record CreateProspectRequest(
    string Name,
    List<string>? Websites,
    string? Notes);
