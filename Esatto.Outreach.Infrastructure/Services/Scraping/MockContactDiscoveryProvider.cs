using Esatto.Outreach.Application.Abstractions;

namespace Esatto.Outreach.Infrastructure.Services.Scraping;

public class MockContactDiscoveryProvider : IContactDiscoveryProvider
{
    public Task<List<ProspectCandidate>> FindDecisionMakersAsync(string companyName, string domain, CancellationToken ct = default)
    {
        // Simulate finding contacts via SERP text processing
        var candidates = new List<ProspectCandidate>
        {
            new ProspectCandidate("Adam Nielsen", "CEO", "https://linkedin.com/in/adamnielsen", "MockSERP", 95),
            new ProspectCandidate("Sarah Marketing", "Marketing Manager", "https://linkedin.com/in/sarahmarketing", "MockSERP", 80),
            new ProspectCandidate("John Tech", "CTO", null, "MockSERP", 70) // Null URL to simulate imperfect data
        };

        return Task.FromResult(candidates);
    }
}
