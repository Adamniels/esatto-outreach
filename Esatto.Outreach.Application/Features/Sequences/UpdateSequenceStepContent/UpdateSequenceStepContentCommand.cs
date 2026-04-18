namespace Esatto.Outreach.Application.Features.Sequences.UpdateSequenceStepContent;

public sealed record UpdateSequenceStepContentCommand(
    Guid SequenceId,
    Guid StepId,
    string? GeneratedSubject,
    string? GeneratedBody
);
