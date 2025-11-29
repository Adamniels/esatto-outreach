using Esatto.Outreach.Infrastructure.Common;

namespace Esatto.Outreach.Infrastructure.EmailGeneration;

/// <summary>
/// Configuration options for email generation method selection.
/// </summary>
public sealed class EmailGenerationOptions
{
    public const string SectionName = "EmailGeneration";

    /// <summary>
    /// Which method to use for email generation operations.
    /// Default is WebSearch to maintain backward compatibility.
    /// </summary>
    public EmailGenerationType DefaultType { get; set; } = EmailGenerationType.WebSearch;
}
