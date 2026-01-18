using System.Threading;
using System.Threading.Tasks;

namespace Esatto.Outreach.Application.Abstractions;

public interface IGenerativeAIClient
{
    Task<string> GenerateTextAsync(
        string userInput,
        string? systemPrompt = null,
        bool useWebSearch = false,
        double temperature = 0.3,
        int maxOutputTokens = 1500,
        CancellationToken ct = default
    );
}
