using Esatto.Outreach.Domain.Enums;

namespace Esatto.Outreach.Application.Features.Sequences.UpdateSequenceStep;

public sealed record UpdateSequenceStepCommand(
    Guid SequenceId,
    Guid StepId,
    SequenceStepType StepType,
    int DelayInDays,
    TimeOfDay? TimeOfDayToRun,
    OutreachGenerationType? GenerationType
);
