using Esatto.Outreach.Domain.Entities;
using Esatto.Outreach.Application.DTOs;
using Esatto.Outreach.Application.DTOs.Prospects;
using Esatto.Outreach.Application.DTOs.Auth;
using Esatto.Outreach.Application.DTOs.Intelligence;
using Esatto.Outreach.Application.DTOs.Outreach;
using Esatto.Outreach.Application.DTOs.Webhooks;
using Esatto.Outreach.Application.DTOs.Workflows;
using Esatto.Outreach.Domain.Enums;

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
