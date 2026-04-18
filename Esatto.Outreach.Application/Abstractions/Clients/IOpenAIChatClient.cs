using Esatto.Outreach.Application.Features.Intelligence;

namespace Esatto.Outreach.Application.Abstractions.Clients;

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
