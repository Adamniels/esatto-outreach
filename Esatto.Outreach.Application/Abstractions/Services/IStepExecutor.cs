using Esatto.Outreach.Domain.Entities;
using Esatto.Outreach.Domain.Entities.SequenceFeature;
using Esatto.Outreach.Domain.Enums;

namespace Esatto.Outreach.Application.Abstractions.Services;

public record StepExecutionContext(
    SequenceStep Step,
    SequenceProspect SequenceProspect,
    Prospect Prospect,
    ContactPerson Contact
);

public interface IStepExecutor
{
    SequenceStepType StepType { get; }
    Task ExecuteAsync(StepExecutionContext context, CancellationToken ct);
}
