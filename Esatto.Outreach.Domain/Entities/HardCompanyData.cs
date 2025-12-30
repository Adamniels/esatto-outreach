using Esatto.Outreach.Domain.Common;

namespace Esatto.Outreach.Domain.Entities;

public class HardCompanyData : Entity
{
    // Statisk företagsinformation
    public string? CompanyOverview { get; private set; }
    
    // JSON-fält för strukturerad data
    // Services: array av tjänster/produkter
    public string? ServicesJson { get; private set; }
    
    // Cases: array av tidigare projekt/case studies
    public string? CasesJson { get; private set; }
    
    // Industries: array av branscher företaget arbetar inom
    public string? IndustriesJson { get; private set; }
    
    // KeyFacts: array av viktiga fakta (grundat år, antal anställda, huvudkontor, etc)
    public string? KeyFactsJson { get; private set; }
    
    // Sources: array av URL:er/referenser där informationen hittades
    public string? SourcesJson { get; private set; }
    
    // När research gjordes
    public DateTime ResearchedAt { get; private set; }

    // EF Core kräver parameterlös ctor
    protected HardCompanyData() { }

    // Fabriksmetod för att skapa ny research
    public static HardCompanyData Create(
        string? companyOverview = null,
        string? servicesJson = null,
        string? casesJson = null,
        string? industriesJson = null,
        string? keyFactsJson = null,
        string? sourcesJson = null)
    {
        var data = new HardCompanyData
        {
            CompanyOverview = companyOverview,
            ServicesJson = servicesJson,
            CasesJson = casesJson,
            IndustriesJson = industriesJson,
            KeyFactsJson = keyFactsJson,
            SourcesJson = sourcesJson,
            ResearchedAt = DateTime.UtcNow
        };

        return data;
    }

    // Metod för att uppdatera befintlig research (istället för att skapa ny rad)
    public void UpdateResearch(
        string? companyOverview = null,
        string? servicesJson = null,
        string? casesJson = null,
        string? industriesJson = null,
        string? keyFactsJson = null,
        string? sourcesJson = null)
    {
        // Uppdatera fält som skickas in (null betyder "uppdatera inte")
        if (companyOverview is not null)
            CompanyOverview = companyOverview;

        if (servicesJson is not null)
            ServicesJson = servicesJson;

        if (casesJson is not null)
            CasesJson = casesJson;

        if (industriesJson is not null)
            IndustriesJson = industriesJson;

        if (keyFactsJson is not null)
            KeyFactsJson = keyFactsJson;

        if (sourcesJson is not null)
            SourcesJson = sourcesJson;

        ResearchedAt = DateTime.UtcNow;
        Touch(); // Uppdatera UpdatedUtc från Entity base class
    }

    // Helper för att kolla om data är gammal (>90 dagar)
    public bool IsStale(int maxAgeDays = 90)
    {
        return (DateTime.UtcNow - ResearchedAt).TotalDays > maxAgeDays;
    }
}
