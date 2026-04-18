using Esatto.Outreach.Application.Features.Sequences.Shared;

namespace Esatto.Outreach.Application.Features.Sequences.UpdateSequence;

public record UpdateSequenceRequest(
    string Title,
    string? Description,
    SequenceSettingsDto? Settings
);
