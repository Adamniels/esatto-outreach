using Esatto.Outreach.Domain.Entities;
using Esatto.Outreach.Application.DTOs;
using Esatto.Outreach.Domain.Enums;

namespace Esatto.Outreach.Application.Abstractions;

/// <summary>
/// Generator for creating custom email drafts.
/// Follows Clean Architecture: Receives all data via context, focuses only on AI interaction.
/// </summary>
public interface ICustomEmailGenerator
{
    /// <summary>
    /// Generates an email draft using the provided context.
    /// </summary>
    /// <param name="context">All data needed for generation (prepared by use case)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Generated email draft</returns>
    Task<CustomEmailDraftDto> GenerateAsync(
        EmailGenerationContext context,
        CancellationToken cancellationToken = default);
}
