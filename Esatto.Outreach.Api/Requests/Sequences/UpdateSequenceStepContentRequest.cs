namespace Esatto.Outreach.Api.Requests.Sequences;

public sealed record UpdateSequenceStepContentRequest(
    string? GeneratedSubject,
    string? GeneratedBody
);
