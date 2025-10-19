using Esatto.Outreach.Application.Abstractions;
using Esatto.Outreach.Application.DTOs;
using Microsoft.Extensions.Options;

namespace Esatto.Outreach.Application.UseCases;

public sealed class ChatWithProspect
{
    private readonly IProspectRepository _repo;
    private readonly IOpenAIChatClient _chat;

    public ChatWithProspect(
        IProspectRepository repo,
        IOpenAIChatClient chat)
    {
        _repo = repo;
        _chat = chat;
    }

    public async Task<ChatResponseDto> Handle(Guid prospectId, ChatRequestDto req, CancellationToken ct = default)
    {
        var prospect = await _repo.GetByIdAsync(prospectId, ct)
            ?? throw new InvalidOperationException("Prospect not found");

        // Will be added to Prospect in next step
        var previousId = prospect.LastOpenAIResponseId;

        // Only include system prompt if no previous response
        var systemPrompt = previousId is null ? GetSystemPrompt() : null;


        (string text, string newResponseId) = await _chat.SendChatMessageAsync(
            userInput: req.UserInput,
            systemPrompt: systemPrompt,
            previousResponseId: previousId,
            useWebSearch: req.UseWebSearch,
            temperature: req.Temperature,
            maxOutputTokens: req.MaxOutputTokens,
            ct: ct
        );

        // Persist new response id on prospect (next step will add this)
        prospect.SetLastOpenAIResponseId(newResponseId);
        await _repo.UpdateAsync(prospect, ct);

        return new ChatResponseDto(text);
    }

    private static string GetSystemPrompt()
    {
        return """
          Du är en hjälpfull AI-assistent i en terminalchatt.
          - Svara kort, korrekt och med steg-för-steg när relevant.
          - Använd web_search-verktyget när frågan kräver färsk information, fakta, nyheter eller osäkra detaljer.
          - När du använder web_search, sammanfatta källor mycket kort.
          """;
    }

}
