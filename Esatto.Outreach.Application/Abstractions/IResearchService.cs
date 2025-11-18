using Esatto.Outreach.Application.DTOs;

namespace Esatto.Outreach.Application.Abstractions;

/// <summary>
/// Provider-agnostic interface for company research services.
/// Can be implemented by OpenAI, Claude, or Hybrid research services.
/// </summary>
public interface IResearchService
{
    /// <summary>
    /// Generates soft company data research including hooks, events, news, and social activity.
    /// </summary>
    /// <param name="companyName">The name of the company to research</param>
    /// <param name="domain">Optional domain/website of the company</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Soft company data with research results</returns>
    Task<SoftCompanyDataDto> GenerateCompanyResearchAsync(
        string companyName,
        string? domain,
        CancellationToken ct = default);
}
