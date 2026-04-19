namespace Esatto.Outreach.Api.Requests.Sequences;

public sealed record ReorderSequenceStepsRequest(List<Guid> StepIdsInOrder);
