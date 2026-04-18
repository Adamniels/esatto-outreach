namespace Esatto.Outreach.Application.Features.Sequences.ReorderSequenceSteps;

public sealed record ReorderSequenceStepsCommand(
    Guid SequenceId,
    List<Guid> StepIdsInOrder
);
