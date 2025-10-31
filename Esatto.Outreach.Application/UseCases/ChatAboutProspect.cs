using Esatto.Outreach.Application.Abstractions;
using Esatto.Outreach.Application.DTOs;
using Esatto.Outreach.Domain.Entities;
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

        // TODO: nu skickar jag med backend mejlet varje gång, men vill skicka med frontend mejlet ifall det uppdaterats, så kanske behöver ta med det från fronted
        var initialMailContext = BuildInitialMailContext(prospect);

        // Only include system prompt if no previous response
        var systemPrompt = previousId is null ? GetSystemPrompt() : null;

        // Lägg till JSON-instruktioner direkt i användarens input för att säkerställa att AI följer formatet
        var enhancedInput = req.UserInput + "\n\n" + GetJsonFormatReminder();

        (ChatResponseDto response, string newResponseId) = await _chat.SendChatMessageAsync(
            userInput: enhancedInput,
            systemPrompt: systemPrompt,
            previousResponseId: previousId,
            useWebSearch: req.UseWebSearch,
            temperature: req.Temperature,
            maxOutputTokens: req.MaxOutputTokens,
            initialMailContext: initialMailContext,
            ct: ct
        );

        // Persist new response id on prospect (next step will add this)
        prospect.SetLastOpenAIResponseId(newResponseId);
        await _repo.UpdateAsync(prospect, ct);

        return response;

    }
    private static string BuildInitialMailContext(Prospect prospect)
    {

        return $"""
    Nuvarande mejlutkast:
    ---
    Ämne: {prospect.MailTitle ?? "[saknas]"}
    Body (plaintext):
    {prospect.MailBodyPlain ?? "[saknas]"}
    ---
    """;
    }

    private static string GetJsonFormatReminder()
    {
        return """
        VIKTIGT: Svara ENDAST med ett JSON-objekt i exakt detta format (ingen extra text):
        {
          "AiMessage": "ditt svar/kommentarer här",
          "ImprovedMail": true eller false,
          "MailTitle": "mejltitel eller null",
          "MailBodyPlain": "mejltext eller null",
          "MailBodyHTML": "mejltext i HTML eller null"
        }
        Om du INTE skapar/förbättrar ett mejl: sätt ImprovedMail till false och alla mail-fält till null.
        Om du skapar/förbättrar ett mejl: sätt ImprovedMail till true och fyll i alla mail-fält.
        """;
    }

    // TODO: vill dra ut sånna här saker som är liksom "config saker" som man vill mixa och testa med
    private static string GetSystemPrompt()
    {
        return """
          Du är en hjälpfull AI-assistent i en terminalchatt.
          - Svara kort, korrekt och med steg-för-steg när relevant.
          - Använd web_search-verktyget när frågan kräver färsk information, fakta, nyheter eller osäkra detaljer.
          - När du använder web_search, sammanfatta källor mycket kort.
          
          VIKTIGT: Svara ALLTID med ett JSON-objekt i följande exakta format:
          {
            "AiMessage": "ditt svar här",
            "ImprovedMail": true eller false,
            "MailTitle": "mejltitel eller null om inget mejl",
            "MailBodyPlain": "mejltext i plaintext eller null om inget mejl",
            "MailBodyHTML": "mejltext i HTML eller null om inget mejl"
          }
          
          - Om användaren inte ber om ett mejl, sätt ImprovedMail till false och alla mail-fält till null.
          - Om användaren ber om att skapa/förbättra ett mejl, sätt ImprovedMail till true och fyll i alla mail-fält.
          - AiMessage ska alltid innehålla ditt svar/kommentarer.
          - Inkludera ENDAST JSON, ingen extra text före eller efter.
          """;
    }

}
