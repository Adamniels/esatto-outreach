namespace Esatto.Outreach.Application.Features.Sequences;

public record UpdateSequenceStepContentRequest(
    string? GeneratedSubject,
    string? GeneratedBody
);
