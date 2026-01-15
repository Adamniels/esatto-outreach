using Esatto.Outreach.Application.Abstractions;
using Esatto.Outreach.Application.DTOs;
using Esatto.Outreach.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

namespace Esatto.Outreach.Application.UseCases.Chat;

public sealed class ChatWithProspect
{
    private readonly IProspectRepository _repo;
    private readonly IEntityIntelligenceRepository _enrichmentRepo;
    private readonly IOpenAIChatClient _chat;
    private readonly ILogger<ChatWithProspect> _logger;
    private static string? _esattoCompanyInfo;
    private static readonly object _lock = new();

    public ChatWithProspect(
        IProspectRepository repo,
        IEntityIntelligenceRepository enrichmentRepo,
        IOpenAIChatClient chat,
        ILogger<ChatWithProspect> logger)
    {
        _repo = repo;
        _enrichmentRepo = enrichmentRepo;
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

        var hasDraft = !string.IsNullOrWhiteSpace(prospect.MailBodyPlain) ||
                            !string.IsNullOrWhiteSpace(prospect.MailBodyHTML);
        var effectiveUseWeb = req.UseWebSearch ?? false;

        _logger.LogInformation("Handling chat for prospect {ProspectId}. UseWebSearch={UseWebSearch}, HasDraft ={HasDraft}",
          prospect.Id, effectiveUseWeb, hasDraft);

        // Fetch Intelligence Context
        string entityContext = "";
        if (prospect.EntityIntelligenceId.HasValue)
        {
            var entity = await _enrichmentRepo.GetByIdAsync(prospect.EntityIntelligenceId.Value, ct);
            if (entity != null)
            {
               var dto = EntityIntelligenceDto.FromEntity(entity);
               entityContext = FormatEntityContext(dto);
            }
        }

        var previousId = prospect.LastOpenAIResponseId;
        var initialMailContext = BuildInitialMailContext(req.MailTitle, req.MailBodyPlain);
        
        // Pass entity context to system prompt
        var systemPrompt = previousId is null ? GetSystemPrompt(entityContext) : null;

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

    private static string GetSystemPrompt(string entityContext)
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
          
          === FAKTA OM PROSPEKTET (ENTITY INTELLIGENCE) ===
          {entityContext}
          
          === INSTRUKTIONER ===
          - Svara kort, korrekt och med steg-för-steg när relevant.
          - Använd web_search-verktyget när frågan kräver färsk information, fakta, nyheter eller osäkra detaljer.
          - När du använder web_search, sammanfatta källor mycket kort.
          - När du skapar eller förbättrar mejl:
            * ANVÄND informationen under 'FAKTA OM PROSPEKTET' för att hitta matchande cases och behov.
            * Hitta relevanta cases som liknar kundens bransch eller utmaningar.
            * Visa konkret förståelse för kundens behov.
            * Matcha rätt tjänster och metoder till kundens situation.
            * Skriv i Esattos ton och värderingar.
          
          VIKTIGT: Svara ALLTID med ett JSON-objekt i följande exakta format:
          {jsonFormat}
          
          - Om användaren inte ber om ett mejl, sätt ImprovedMail till false och alla mail-fält till null.
          - Om användaren ber om att skapa/förbättra ett mejl, sätt ImprovedMail till true och fyll i alla mail-fält.
          - AiMessage ska alltid innehålla ditt svar/kommentarer.
          - Inkludera ENDAST JSON, ingen extra text före eller efter.
          """;
    }

    private string FormatEntityContext(EntityIntelligenceDto dto)
    {
        var sb = new System.Text.StringBuilder();
        
        if (dto.RichData != null)
        {
            var rd = dto.RichData;
            sb.AppendLine($"SUMMARY: {rd.Summary}");
            
            if (rd.KeyValueProps?.Any() == true)
                sb.AppendLine($"VALUE PROPS: {string.Join(", ", rd.KeyValueProps)}");
                
            if (rd.TechStack?.Any() == true)
                sb.AppendLine($"TECH STACK: {string.Join(", ", rd.TechStack)}");
                
            if (rd.CaseStudies?.Any() == true)
            {
                sb.AppendLine("CASE STUDIES:");
                foreach(var c in rd.CaseStudies)
                {
                   sb.AppendLine($"- Client: {c.Client} | Challenge: {c.Challenge} | Solution: {c.Solution} | Outcome: {c.Outcome}");
                }
            }
            
            if (rd.News?.Any() == true)
            {
                sb.AppendLine("RECENT NEWS:");
                foreach(var n in rd.News) sb.AppendLine($"- {n.Date}: {n.Description}");
            }
        }
        else
        {
           sb.AppendLine($"SUMMARY: {dto.SummarizedContext}");
           if (dto.CompanyHooks?.Any() == true)
           {
               sb.AppendLine("HOOKS:");
               foreach(var h in dto.CompanyHooks) sb.AppendLine($"- {h}");
           }
        }
        
        return sb.ToString();
    }

}
