using Esatto.Outreach.Domain.Common;

namespace Esatto.Outreach.Domain.Entities;

public class SoftCompanyData : Entity
{
    // Foreign key till Prospect
    public Guid ProspectId { get; private set; }
    
    // Navigation property till Prospect
    public Prospect? Prospect { get; private set; }
    
    // JSON-fält för dynamisk, tidskänslig data

    // Hooks: array av personliga hooks för mejl-öppnare
    // Ex: [{ "text": "Såg att ni höll event om AI förra veckan", "source": "LinkedIn", "date": "2025-11-06", "relevance": "high" }]
    public string? HooksJson { get; private set; }

    // RecentEvents: array av events/webinars/konferenser företaget hållit/deltagit i
    // Ex: [{ "title": "AI Summit 2025", "date": "2025-11-05", "type": "webinar", "url": "..." }]
    public string? RecentEventsJson { get; private set; }

    // NewsItems: array av nyhetsartiklar/pressmeddelanden om företaget
    // Ex: [{ "headline": "Företag X lanserar ny produkt", "date": "2025-11-01", "source": "TechCrunch", "url": "..." }]
    public string? NewsItemsJson { get; private set; }

    // SocialActivity: array av intressanta inlägg från LinkedIn/Twitter
    // Ex: [{ "platform": "LinkedIn", "text": "Stolta över vårt nya partnerskap...", "date": "2025-11-03", "url": "..." }]
    public string? SocialActivityJson { get; private set; }

    // Sources: array av URL:er/referenser där informationen hittades
    public string? SourcesJson { get; private set; }

    // När research gjordes
    public DateTime ResearchedAt { get; private set; }


    // EF Core kräver parameterlös ctor
    protected SoftCompanyData() { }

    public static SoftCompanyData Create(
        Guid prospectId,
        string? hooksJson,
        string? recentEventsJson,
        string? newsItemsJson,
        string? socialActivityJson,
        string? sourcesJson)
    {
        var data = new SoftCompanyData
        {
            ProspectId = prospectId,
            HooksJson = hooksJson,
            RecentEventsJson = recentEventsJson,
            NewsItemsJson = newsItemsJson,
            SocialActivityJson = socialActivityJson,
            SourcesJson = sourcesJson,
            ResearchedAt = DateTime.UtcNow
        };

        return data;
    }

    public void UpdateResearch(
           string? hooksJson = null,
           string? recentEventsJson = null,
           string? newsItemsJson = null,
           string? socialActivityJson = null,
           string? sourcesJson = null)
    {
        // Uppdatera fält som skickas in (null betyder "uppdatera inte")
        if (hooksJson is not null)
            HooksJson = hooksJson;

        if (recentEventsJson is not null)
            RecentEventsJson = recentEventsJson;

        if (newsItemsJson is not null)
            NewsItemsJson = newsItemsJson;

        if (socialActivityJson is not null)
            SocialActivityJson = socialActivityJson;

        if (sourcesJson is not null)
            SourcesJson = sourcesJson;

        ResearchedAt = DateTime.UtcNow;
        Touch(); // Uppdatera UpdatedUtc från Entity base class
    }

    // Helper för att kolla om data är gammal (standard 7 dagar för mjuk data)
    public bool IsStale(int maxAgeDays = 7)
    {
        return (DateTime.UtcNow - ResearchedAt).TotalDays > maxAgeDays;
    }
}

