namespace Esatto.Outreach.Application.DTOs.Prospects;

public record ProspectCreateDto(
    string Name,
    List<string>? Websites,
    string? Notes);
