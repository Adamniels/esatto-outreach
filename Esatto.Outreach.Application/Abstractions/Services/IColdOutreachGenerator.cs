using Esatto.Outreach.Application.Features.OutreachGeneration.Shared;

namespace Esatto.Outreach.Application.Abstractions.Services;

public interface IColdOutreachGenerator
{
    Task<CustomOutreachDraftDto> GenerateAsync(ColdOutreachContext context, CancellationToken ct = default);
}
