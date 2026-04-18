using Esatto.Outreach.Domain.Entities;
using Esatto.Outreach.Domain.ValueObjects;

namespace Esatto.Outreach.Application.Features.Intelligence.Shared;

public record EntityIntelligenceDto(
    Guid Id,
    Guid ProspectId,
    string? SummarizedContext,
    string? EnrichmentVersion,
    CompanyEnrichmentResult? EnrichedData,
    DateTime ResearchedAt,
    DateTime CreatedUtc,
    DateTime? UpdatedUtc,
    List<string> CompanyHooks
)
{
    public static EntityIntelligenceDto FromEntity(EntityIntelligence entity)
    {
        return new(
            Id: entity.Id,
            ProspectId: entity.ProspectId,
            SummarizedContext: entity.SummarizedContext,
            EnrichmentVersion: entity.EnrichmentVersion,
            EnrichedData: entity.EnrichedData,
            ResearchedAt: entity.ResearchedAt,
            CreatedUtc: entity.CreatedUtc,
            UpdatedUtc: entity.UpdatedUtc,
            CompanyHooks: entity.EnrichedData?.OutreachHooks?.Select(h => h.HookDescription).ToList() ?? new()
        );
    }
}
