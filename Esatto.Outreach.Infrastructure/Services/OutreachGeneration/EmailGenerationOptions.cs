using Esatto.Outreach.Infrastructure.Options;


namespace Esatto.Outreach.Infrastructure.Services.OutreachGeneration;

/// <summary>
/// Configuration options for email generation method selection.
/// </summary>
// TODO: Should rename this OutreachGenerationOptions
public sealed class EmailGenerationOptions
{
    public const string SectionName = "EmailGeneration";

    /// <summary>
    /// Which method to use for email generation operations.
    /// Default is WebSearch to maintain backward compatibility.
    /// </summary>
    public EmailGenerationType DefaultType { get; set; } = EmailGenerationType.WebSearch;
}
