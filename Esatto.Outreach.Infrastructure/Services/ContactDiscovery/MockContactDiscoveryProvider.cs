using Esatto.Outreach.Application.Abstractions.Services;

namespace Esatto.Outreach.Infrastructure.Services.ContactDiscovery;

public class MockContactDiscoveryProvider : IContactDiscoveryProvider
{
    public Task<List<ProspectCandidate>> FindDecisionMakersAsync(string companyName, string domain, CancellationToken ct = default)
    {
        var candidates = new List<ProspectCandidate>
        {
            new ProspectCandidate("Adam Nielsen", "CEO", "https://linkedin.com/in/adamnielsen", "MockSERP", 95),
            new ProspectCandidate("Sarah Marketing", "Marketing Manager", "https://linkedin.com/in/sarahmarketing", "MockSERP", 80),
            new ProspectCandidate("John Tech", "CTO", null, "MockSERP", 70)
        };

        return Task.FromResult(candidates);
    }
}
