
using System.Threading;
using System.Threading.Tasks;
using Esatto.Outreach.Application.DTOs;

namespace Esatto.Outreach.Application.Abstractions;

public interface IOpenAIChatClient
{
    Task<(ChatResponseDto response, string ResponseId)> SendChatMessageAsync(
    string userInput,
    string? systemPrompt,
    string? previousResponseId,
    bool? useWebSearch,
    double? temperature,
    int? maxOutputTokens,
    string? initialMailContext,
    CancellationToken ct = default
    );
}
