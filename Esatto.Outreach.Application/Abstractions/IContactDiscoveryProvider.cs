using Esatto.Outreach.Domain.Entities;

namespace Esatto.Outreach.Application.Abstractions;

public interface IContactDiscoveryProvider
{
    /// <summary>
    /// Finds potential contacts for a company that match specific roles.
    /// </summary>
    Task<List<ProspectCandidate>> FindDecisionMakersAsync(string companyName, string domain, CancellationToken ct = default);
}

public record ProspectCandidate(
    string Name, 
    string Title, 
    string? LinkedInUrl, 
    string Source, 
    int ConfidenceScore
);
