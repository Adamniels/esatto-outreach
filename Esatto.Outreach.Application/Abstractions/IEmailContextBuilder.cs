using Esatto.Outreach.Application.DTOs;

namespace Esatto.Outreach.Application.Abstractions;

/// <summary>
/// Builder for creating email generation context with all required data.
/// Orchestrates data fetching from repositories and file system.
/// </summary>
public interface IEmailContextBuilder
{
    /// <summary>
    /// Builds email generation context with all required data.
    /// </summary>
    /// <param name="prospectId">ID of the prospect to generate email for</param>
    /// <param name="userId">ID of the user generating the email</param>
    /// <param name="includeSoftData">Whether to include soft company data in context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Context with all data needed for email generation</returns>
    /// <exception cref="InvalidOperationException">Thrown when required data is missing</exception>
    Task<EmailGenerationContext> BuildContextAsync(
        Guid prospectId,
        string userId,
        bool includeSoftData,
        CancellationToken cancellationToken = default);
}
