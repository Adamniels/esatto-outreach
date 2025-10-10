using Microsoft.Extensions.Options;
using OpenAI.Responses;

namespace Esatto.Outreach.Infrastructure.Email;

// NOTE: detta är experimentel så kanske breakar, suppresed warnings in .csproj
public interface IOpenAIResponseClientFactory
{
    OpenAIResponseClient GetClient();
}

public sealed class OpenAIResponseClientFactory : IOpenAIResponseClientFactory
{
    private readonly OpenAiOptions _options;

    public OpenAIResponseClientFactory(IOptions<OpenAiOptions> options)
        => _options = options.Value;

    public OpenAIResponseClient GetClient()
        => new OpenAIResponseClient(
            model: _options.Model,
            apiKey: _options.ApiKey
        );
}
