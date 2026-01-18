using Esatto.Outreach.Domain.Common;

namespace Esatto.Outreach.Domain.Entities;

public class EntityIntelligence : Entity
{
    // Foreign key till Prospect
    public Guid ProspectId { get; private set; }
    
    // Navigation property till Prospect
    public Prospect? Prospect { get; private set; }
    
    // === NEW ENRICHMENT FIELDS ===
    // "CEO Adam has explicitly mentioned a need for 'modernizing legacy tech' in his recent interview..."
    public string? SummarizedContext { get; private set; }

    public string? EnrichmentVersion { get; private set; } // e.g. "v2-company-only"
    
    // Structured Enrichment Data (Mapped to JSON in DB)
    public Esatto.Outreach.Domain.ValueObjects.CompanyEnrichmentResult? EnrichedData { get; private set; }

    // When the research was performed
    public DateTime ResearchedAt { get; private set; }


    // EF Core requires parameterless ctor
    protected EntityIntelligence() { }

    public static EntityIntelligence Create(
        Guid prospectId,
        string? summarizedContext,
        Esatto.Outreach.Domain.ValueObjects.CompanyEnrichmentResult? enrichedData,
        string? enrichmentVersion = null)
    {
        return new EntityIntelligence
        {
            ProspectId = prospectId,
            SummarizedContext = summarizedContext,
            EnrichedData = enrichedData,
            EnrichmentVersion = enrichmentVersion,
            ResearchedAt = DateTime.UtcNow
        };
    }

    public void UpdateResearch(
           string? summarizedContext = null,
           Esatto.Outreach.Domain.ValueObjects.CompanyEnrichmentResult? enrichedData = null,
           string? enrichmentVersion = null)
    {
        if (summarizedContext is not null) SummarizedContext = summarizedContext;
        if (enrichedData is not null) EnrichedData = enrichedData;
        if (enrichmentVersion is not null) EnrichmentVersion = enrichmentVersion;

        ResearchedAt = DateTime.UtcNow;
        Touch();
    }

    // Standard stale check (default 14 days for deep research)
    public bool IsStale(int maxAgeDays = 14)
    {
        return (DateTime.UtcNow - ResearchedAt).TotalDays > maxAgeDays;
    }
}

