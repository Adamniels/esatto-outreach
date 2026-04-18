namespace Esatto.Outreach.Application.Features.Prospects;

public record ProspectCreateDto(
    string Name,
    List<string>? Websites,
    string? Notes);
