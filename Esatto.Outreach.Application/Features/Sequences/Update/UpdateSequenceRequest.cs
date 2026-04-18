namespace Esatto.Outreach.Application.Features.Sequences;

public record UpdateSequenceRequest(
    string Title,
    string? Description,
    SequenceSettingsDto? Settings
);
