using Esatto.Outreach.Domain.Enums;

namespace Esatto.Outreach.Application.Features.Sequences.AddSequenceStep;

public sealed record AddSequenceStepCommand(
    Guid SequenceId,
    SequenceStepType StepType,
    int DelayInDays,
    TimeOfDay? TimeOfDayToRun,
    OutreachGenerationType? GenerationType
);
