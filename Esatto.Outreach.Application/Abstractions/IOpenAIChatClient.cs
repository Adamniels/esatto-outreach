
using System.Threading;
using System.Threading.Tasks;

namespace Esatto.Outreach.Application.Abstractions;

public interface IOpenAIChatClient
{
    Task<(string Text, string ResponseId)> SendChatMessageAsync(
    string userInput,
    string? systemPrompt,
    string? previousResponseId,
    bool? useWebSearch,
    double? temperature,
    int? maxOutputTokens,
    CancellationToken ct = default
    );
}
