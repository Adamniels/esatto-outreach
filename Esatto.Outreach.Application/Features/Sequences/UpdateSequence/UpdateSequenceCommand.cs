using Esatto.Outreach.Application.Features.Sequences.Shared;

namespace Esatto.Outreach.Application.Features.Sequences.UpdateSequence;

public sealed record UpdateSequenceCommand(
    Guid Id,
    string Title,
    string? Description,
    SequenceSettingsDto? Settings
);
