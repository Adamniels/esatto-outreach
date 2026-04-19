using Esatto.Outreach.Domain.Enums;

namespace Esatto.Outreach.Api.Requests.Sequences;

public sealed record CreateSequenceRequest(
    string Title,
    string? Description,
    SequenceMode Mode
);
