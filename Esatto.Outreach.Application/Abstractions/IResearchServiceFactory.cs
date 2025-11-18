namespace Esatto.Outreach.Application.Abstractions;

/// <summary>
/// Factory for creating the appropriate research service based on configuration.
/// Implements Strategy Pattern for provider selection.
/// </summary>
public interface IResearchServiceFactory
{
    /// <summary>
    /// Gets the configured research service instance (OpenAI, Claude, or Hybrid).
    /// </summary>
    /// <returns>Research service implementation based on configuration</returns>
    IResearchService GetResearchService();
}
