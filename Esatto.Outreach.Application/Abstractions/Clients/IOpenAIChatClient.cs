
using System.Threading;
using System.Threading.Tasks;
using Esatto.Outreach.Application.DTOs;
using Esatto.Outreach.Application.DTOs.Prospects;
using Esatto.Outreach.Application.DTOs.Auth;
using Esatto.Outreach.Application.DTOs.Intelligence;
using Esatto.Outreach.Application.DTOs.Outreach;
using Esatto.Outreach.Application.DTOs.Webhooks;
using Esatto.Outreach.Application.DTOs.Workflows;

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
