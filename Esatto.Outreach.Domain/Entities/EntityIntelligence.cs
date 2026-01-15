using Esatto.Outreach.Domain.Common;

namespace Esatto.Outreach.Domain.Entities;

public class EntityIntelligence : Entity
{
    // Foreign key till Prospect
    public Guid ProspectId { get; private set; }
    
    // Navigation property till Prospect
    public Prospect? Prospect { get; private set; }
    
    // JSON: List<string> of company-level news/events
    // Ex: ["Launched new AI feature", "Opening office in London"]
    public string? CompanyHooksJson { get; private set; }

    // JSON: List<string> of personal-level hooks
    // Ex: ["Host of the 'Digital Future' podcast", "Posted about React 19 on LinkedIn"]
    public string? PersonalHooksJson { get; private set; }

    // A synthesized summary of why this person/company is a good fit
    // "CEO Adam has explicitly mentioned a need for 'modernizing legacy tech' in his recent interview..."
    public string? SummarizedContext { get; private set; }

    // JSON: List of sources used for verification
    // Ex: [{ "url": "https://linkedin.com/in/...", "type": "profile" }]
    public string? SourcesJson { get; private set; }

    // When the research was performed
    public DateTime ResearchedAt { get; private set; }


    // EF Core requires parameterless ctor
    protected EntityIntelligence() { }

    public static EntityIntelligence Create(
        Guid prospectId,
        string? companyHooksJson,
        string? personalHooksJson,
        string? summarizedContext,
        string? sourcesJson)
    {
        return new EntityIntelligence
        {
            ProspectId = prospectId,
            CompanyHooksJson = companyHooksJson,
            PersonalHooksJson = personalHooksJson,
            SummarizedContext = summarizedContext,
            SourcesJson = sourcesJson,
            ResearchedAt = DateTime.UtcNow
        };
    }

    public void UpdateResearch(
           string? companyHooksJson = null,
           string? personalHooksJson = null,
           string? summarizedContext = null,
           string? sourcesJson = null)
    {
        if (companyHooksJson is not null) CompanyHooksJson = companyHooksJson;
        if (personalHooksJson is not null) PersonalHooksJson = personalHooksJson;
        if (summarizedContext is not null) SummarizedContext = summarizedContext;
        if (sourcesJson is not null) SourcesJson = sourcesJson;

        ResearchedAt = DateTime.UtcNow;
        Touch();
    }

    // Standard stale check (default 14 days for deep research)
    public bool IsStale(int maxAgeDays = 14)
    {
        return (DateTime.UtcNow - ResearchedAt).TotalDays > maxAgeDays;
    }
}

