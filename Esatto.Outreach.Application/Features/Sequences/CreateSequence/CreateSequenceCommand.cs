using Esatto.Outreach.Domain.Enums;

namespace Esatto.Outreach.Application.Features.Sequences.CreateSequence;

public sealed record CreateSequenceCommand(
    string Title,
    string? Description,
    SequenceMode Mode
);
