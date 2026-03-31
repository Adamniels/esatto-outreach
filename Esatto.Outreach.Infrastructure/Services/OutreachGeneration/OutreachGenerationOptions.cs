using Esatto.Outreach.Domain.Enums;

namespace Esatto.Outreach.Infrastructure.Services.OutreachGeneration;

/// <summary>
/// Configuration options for outreach generation method selection.
/// </summary>
public sealed class OutreachGenerationOptions
{
    public const string SectionName = "OutreachGeneration";

    /// <summary>
    /// Which method to use for generation operations.
    /// Default is WebSearch to maintain backward compatibility.
    /// </summary>
    public OutreachGenerationType DefaultType { get; set; } = OutreachGenerationType.WebSearch;
}
