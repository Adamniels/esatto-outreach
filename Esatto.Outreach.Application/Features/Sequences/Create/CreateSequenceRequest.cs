using Esatto.Outreach.Domain.Enums;

namespace Esatto.Outreach.Application.Features.Sequences;

public record CreateSequenceRequest(
    string Title,
    string? Description,
    SequenceMode Mode
);
