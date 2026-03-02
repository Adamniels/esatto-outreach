using Esatto.Outreach.Application.DTOs;

using Esatto.Outreach.Domain.Enums;

namespace Esatto.Outreach.Application.Abstractions;

/// <summary>
/// Builder for creating outreach generation context with all required data.
/// Orchestrates data fetching from repositories and file system.
/// </summary>
public interface IOutreachContextBuilder
{
    /// <summary>
    /// Builds outreach generation context with all required data.
    /// </summary>
    Task<OutreachGenerationContext> BuildContextAsync(
        Guid prospectId,
        string userId,
        OutreachChannel channel,
        bool includeSoftData,
        CancellationToken cancellationToken = default);
}
