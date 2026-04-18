using Esatto.Outreach.Domain.Enums;

namespace Esatto.Outreach.Application.Features.Sequences.UpdateSequenceStep;

public record UpdateSequenceStepRequest(
    SequenceStepType StepType,
    int DelayInDays,
    TimeOfDay? TimeOfDayToRun,
    OutreachGenerationType? GenerationType
);
