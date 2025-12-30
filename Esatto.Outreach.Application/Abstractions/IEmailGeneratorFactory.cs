namespace Esatto.Outreach.Application.Abstractions;

/// <summary>
/// Factory for creating the appropriate email generator based on the generation type.
/// </summary>
public interface IEmailGeneratorFactory
{
    /// <summary>
    /// Gets the default email generator based on configuration.
    /// </summary>
    ICustomEmailGenerator GetGenerator();

    /// <summary>
    /// Gets the appropriate email generator based on the specified type.
    /// </summary>
    /// <param name="type">The type of email generation (e.g., "WebSearch", "UseCollectedData")</param>
    /// <returns>An instance of ICustomEmailGenerator</returns>
    /// <exception cref="ArgumentException">Thrown when the type is not valid</exception>
    ICustomEmailGenerator GetGenerator(string type);
}
