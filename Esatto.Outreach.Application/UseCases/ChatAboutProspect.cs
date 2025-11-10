using Esatto.Outreach.Application.Abstractions;
using Esatto.Outreach.Application.DTOs;
using Esatto.Outreach.Domain.Entities;
using Microsoft.Extensions.Options;

namespace Esatto.Outreach.Application.UseCases;
using Microsoft.Extensions.Logging;

public sealed class ChatWithProspect
{
    private readonly IProspectRepository _repo;
    private readonly IOpenAIChatClient _chat;
    private readonly ILogger<ChatWithProspect> _logger;
    private static string? _esattoCompanyInfo;
    private static readonly object _lock = new();

    public ChatWithProspect(
        IProspectRepository repo,
        IOpenAIChatClient chat,
        ILogger<ChatWithProspect> logger)
    {
        _repo = repo;
        _chat = chat;
        _logger = logger;
        LoadEsattoCompanyInfo();
    }

    private static void LoadEsattoCompanyInfo()
    {
        if (_esattoCompanyInfo != null) return;
        
        lock (_lock)
        {
            if (_esattoCompanyInfo != null) return;

            try
            {
                var filePath = Path.Combine(AppContext.BaseDirectory, "Data", "esatto-company-info.json");
                if (File.Exists(filePath))
                {
                    _esattoCompanyInfo = File.ReadAllText(filePath);
                }
                else
                {
                    _esattoCompanyInfo = "{}"; // Fallback om filen inte hittas
                }
            }
            catch
            {
                _esattoCompanyInfo = "{}";
            }
        }
    }

    public async Task<ChatResponseDto> Handle(Guid prospectId, ChatRequestDto req, CancellationToken ct = default)
    {
        var prospect = await _repo.GetByIdAsync(prospectId, ct)
            ?? throw new InvalidOperationException("Prospect not found");

        // Trying out logging, pretty straight forward
        var hasDraft = !string.IsNullOrWhiteSpace(prospect.MailBodyPlain) ||
                            !string.IsNullOrWhiteSpace(prospect.MailBodyHTML);
        var effectiveUseWeb = req.UseWebSearch ?? false; // eller läs default från options om du har den

        _logger.LogInformation("Handling chat for prospect {ProspectId}. UseWebSearch={UseWebSearch}, HasDraft ={HasDraft}",
          prospect.Id,
          effectiveUseWeb,
          hasDraft);

        // Will be added to Prospect in next step
        var previousId = prospect.LastOpenAIResponseId;

        // Add the mail that is currently in the frontent, we get the current mail from frontend
        var initialMailContext = BuildInitialMailContext(req.MailTitle, req.MailBodyPlain);

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
    private static string BuildInitialMailContext(string? mailTitle, string? mailBody)
    {

        return $"""
    Nuvarande mejlutkast:
    ---
    Ämne: {mailTitle ?? "[saknas]"}
    Body (plaintext):
    {mailBody ?? "[saknas]"}
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
        var jsonFormat = """
          {
            "AiMessage": "ditt svar här",
            "ImprovedMail": true eller false,
            "MailTitle": "mejltitel eller null om inget mejl",
            "MailBodyPlain": "mejltext i plaintext eller null om inget mejl",
            "MailBodyHTML": "mejltext i HTML eller null om inget mejl"
          }
          """;

        return $"""
          Du är en hjälpfull AI-assistent som hjälper säljare på Esatto AB att skapa och förbättra säljmejl.
          
          === INFORMATION OM ESATTO AB ===
          {_esattoCompanyInfo}
          
          === INSTRUKTIONER ===
          - Svara kort, korrekt och med steg-för-steg när relevant.
          - Använd web_search-verktyget när frågan kräver färsk information, fakta, nyheter eller osäkra detaljer.
          - När du använder web_search, sammanfatta källor mycket kort.
          - När du skapar eller förbättrar mejl, använd informationen om Esatto ovan för att:
            * Hitta relevanta cases som liknar kundens bransch eller utmaningar
            * Visa konkret förståelse för kundens behov genom att referera till liknande projekt
            * Matcha rätt tjänster och metoder till kundens situation
            * Skriv i Esattos ton och värderingar (ärlighet, engagemang, omtanke, samarbete)
          
          VIKTIGT: Svara ALLTID med ett JSON-objekt i följande exakta format:
          {jsonFormat}
          
          - Om användaren inte ber om ett mejl, sätt ImprovedMail till false och alla mail-fält till null.
          - Om användaren ber om att skapa/förbättra ett mejl, sätt ImprovedMail till true och fyll i alla mail-fält.
          - AiMessage ska alltid innehålla ditt svar/kommentarer.
          - Inkludera ENDAST JSON, ingen extra text före eller efter.
          """;
    }

}
