namespace Esatto.Outreach.Application.Features.Sequences.ReorderSequenceSteps;

public record ReorderSequenceStepsRequest(
    List<Guid> StepIdsInOrder
);
