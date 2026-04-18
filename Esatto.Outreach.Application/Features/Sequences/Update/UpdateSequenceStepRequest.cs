using Esatto.Outreach.Domain.Enums;

namespace Esatto.Outreach.Application.Features.Sequences;

public record UpdateSequenceStepRequest(
    SequenceStepType StepType,
    int DelayInDays,
    TimeOfDay? TimeOfDayToRun,
    OutreachGenerationType? GenerationType
);
