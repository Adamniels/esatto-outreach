namespace Esatto.Outreach.Application.Features.Sequences.GenerateStepContent;

public sealed record GenerateStepContentCommand(
    Guid SequenceId,
    Guid StepId
);
