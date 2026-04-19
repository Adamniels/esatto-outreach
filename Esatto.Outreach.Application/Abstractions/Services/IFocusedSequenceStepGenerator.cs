using Esatto.Outreach.Application.Features.OutreachGeneration.Shared;

namespace Esatto.Outreach.Application.Abstractions.Services;

public interface IFocusedSequenceStepGenerator
{
    Task<CustomOutreachDraftDto> GenerateAsync(FocusedSequenceStepContext context, CancellationToken ct = default);
}
