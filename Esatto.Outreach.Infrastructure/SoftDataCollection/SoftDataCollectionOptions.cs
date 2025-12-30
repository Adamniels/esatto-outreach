using Esatto.Outreach.Infrastructure.Common;

namespace Esatto.Outreach.Infrastructure.SoftDataCollection;

/// <summary>
/// Configuration options for soft data collection AI provider selection.
/// </summary>
public sealed class SoftDataCollectionOptions
{
    public const string SectionName = "SoftDataCollection";

    /// <summary>
    /// Which AI provider to use for soft data collection operations.
    /// Default is OpenAI to maintain backward compatibility.
    /// </summary>
    public AiProviderType Provider { get; set; } = AiProviderType.OpenAI;
}
