using Esatto.Outreach.Domain.Enums;

namespace Esatto.Outreach.Api.Requests.Sequences;

public sealed record UpdateSequenceStepRequest(
    SequenceStepType StepType,
    int DelayInDays,
    TimeOfDay? TimeOfDayToRun,
    OutreachGenerationType? GenerationType
);
