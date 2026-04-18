using Esatto.Outreach.Domain.Enums;

namespace Esatto.Outreach.Application.Features.Sequences.CreateSequence;

public record CreateSequenceRequest(
    string Title,
    string? Description,
    SequenceMode Mode
);
