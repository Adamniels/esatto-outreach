using Esatto.Outreach.Application.Features.OutreachGeneration.Shared;
using Esatto.Outreach.Domain.Enums;

namespace Esatto.Outreach.Application.Abstractions.Services;

public interface IColdOutreachContextBuilder
{
    Task<ColdOutreachContext> BuildAsync(
        Guid prospectId,
        string userId,
        OutreachChannel channel,
        OutreachGenerationType strategy,
        CancellationToken ct = default);
}
