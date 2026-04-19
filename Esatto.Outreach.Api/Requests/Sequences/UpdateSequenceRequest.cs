using Esatto.Outreach.Application.Features.Sequences.Shared;

namespace Esatto.Outreach.Api.Requests.Sequences;

public sealed record UpdateSequenceRequest(
    string Title,
    string? Description,
    SequenceSettingsDto? Settings
);
