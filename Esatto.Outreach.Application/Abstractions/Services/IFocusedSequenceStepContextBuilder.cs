using Esatto.Outreach.Application.Features.OutreachGeneration.Shared;
using Esatto.Outreach.Domain.Enums;

namespace Esatto.Outreach.Application.Abstractions.Services;

public interface IFocusedSequenceStepContextBuilder
{
    Task<FocusedSequenceStepContext> BuildAsync(
        Guid prospectId,
        string userId,
        OutreachChannel channel,
        bool includeSoftData,
        int stepNumber,
        int totalSteps,
        SequenceStepType stepType,
        int delayInDays,
        List<PriorTurn> priorTurns,
        CancellationToken ct = default);
}
