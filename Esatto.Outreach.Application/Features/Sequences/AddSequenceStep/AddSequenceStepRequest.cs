using Esatto.Outreach.Domain.Enums;

namespace Esatto.Outreach.Application.Features.Sequences.AddSequenceStep;

public record AddSequenceStepRequest(
    SequenceStepType StepType,
    int DelayInDays,
    TimeOfDay? TimeOfDayToRun,
    OutreachGenerationType? GenerationType
);
