namespace Esatto.Outreach.Application.Abstractions;

/// <summary>
/// Factory for creating the appropriate outreach generator based on the generation type.
/// </summary>
public interface IOutreachGeneratorFactory
{
    /// <summary>
    /// Gets the default outreach generator based on configuration.
    /// </summary>
    IOutreachGenerator GetGenerator();

    /// <summary>
    /// Gets the appropriate outreach generator based on the specified type.
    /// </summary>
    /// <param name="type">The type of outreach generation (e.g., "WebSearch", "UseCollectedData")</param>
    /// <returns>An instance of IOutreachGenerator</returns>
    /// <exception cref="ArgumentException">Thrown when the type is not valid</exception>
    IOutreachGenerator GetGenerator(string type);
}
