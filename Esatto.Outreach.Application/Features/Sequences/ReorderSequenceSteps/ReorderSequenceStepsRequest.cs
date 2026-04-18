namespace Esatto.Outreach.Application.Features.Sequences;

public record ReorderSequenceStepsRequest(
    List<Guid> StepIdsInOrder
);
