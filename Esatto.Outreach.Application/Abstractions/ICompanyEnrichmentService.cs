using Esatto.Outreach.Domain.ValueObjects;

namespace Esatto.Outreach.Application.Abstractions;

public interface ICompanyEnrichmentService
{
    /// <summary>
    /// Performs deep research on a company to extract structured business intelligence.
    /// Does NOT search for contact persons.
    /// </summary>
    /// <param name="companyName">Name of the company</param>
    /// <param name="domain">Website domain (e.g. spotify.com)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Structured enrichment result including snapshot, challenges, profile, and hooks.</returns>
    Task<CompanyEnrichmentResult> EnrichCompanyAsync(string companyName, string domain, CancellationToken ct = default);
}
