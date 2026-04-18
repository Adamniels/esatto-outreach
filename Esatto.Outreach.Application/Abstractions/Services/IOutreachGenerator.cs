using Esatto.Outreach.Application.Features.OutreachGeneration;

namespace Esatto.Outreach.Application.Abstractions.Services;

/// <summary>
/// Generator for creating custom outreach drafts.
/// Follows Clean Architecture: Receives all data via context, focuses only on AI interaction.
/// </summary>
public interface IOutreachGenerator
{
    /// <summary>
    /// Generates an outreach draft using the provided context.
    /// </summary>
    /// <param name="context">All data needed for generation (prepared by use case)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Generated outreach draft</returns>
    Task<CustomOutreachDraftDto> GenerateAsync(
        OutreachGenerationContext context,
        CancellationToken cancellationToken = default);
}
