namespace Esatto.Outreach.Application.Features.Prospects.CreateProspect;

public sealed record CreateProspectCommand(
    string Name,
    List<string>? Websites,
    string? Notes
);
