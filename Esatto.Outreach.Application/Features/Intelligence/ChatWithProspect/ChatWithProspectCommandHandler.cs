using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Application.Abstractions.Clients;
using Esatto.Outreach.Application.Features.Intelligence.Shared;
using Microsoft.Extensions.Logging;

namespace Esatto.Outreach.Application.Features.Intelligence.ChatWithProspect;

public sealed class ChatWithProspectCommandHandler
{
    private readonly IProspectRepository _repo;
    private readonly IEntityIntelligenceRepository _enrichmentRepo;
    private readonly IOpenAIChatClient _chat;
    private readonly ILogger<ChatWithProspectCommandHandler> _logger;
    private static string? _esattoCompanyInfo;

    public ChatWithProspectCommandHandler(
        IProspectRepository repo,
        IEntityIntelligenceRepository enrichmentRepo,
        IOpenAIChatClient chat,
        ILogger<ChatWithProspectCommandHandler> logger)
    {
        _repo = repo;
        _enrichmentRepo = enrichmentRepo;
        _chat = chat;
        _logger = logger;
        LoadEsattoCompanyInfo();
    }

    private static void LoadEsattoCompanyInfo()
    {
        if (_esattoCompanyInfo == null)
        {
            _esattoCompanyInfo = "{}";
        }
    }

    public async Task<ChatWithProspectResponse> Handle(ChatWithProspectCommand command, string userId, CancellationToken ct = default)
    {
        var prospect = await _repo.GetByIdAsync(command.ProspectId, ct)
            ?? throw new InvalidOperationException("Prospect not found");

        if (prospect.OwnerId != userId)
            throw new UnauthorizedAccessException("You don't have permission to access this prospect");

        var hasDraft = !string.IsNullOrWhiteSpace(prospect.MailBodyPlain) ||
                            !string.IsNullOrWhiteSpace(prospect.MailBodyHTML);
        var effectiveUseWeb = command.UseWebSearch ?? false;

        _logger.LogInformation("Handling chat for prospect {ProspectId}. UseWebSearch={UseWebSearch}, HasDraft ={HasDraft}",
          prospect.Id, effectiveUseWeb, hasDraft);

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
        var initialMailContext = BuildInitialMailContext(command.MailTitle, command.MailBodyPlain);

        var systemPrompt = previousId is null ? GetSystemPrompt(entityContext) : null;
        var enhancedInput = command.UserInput + "\n\n" + GetJsonFormatReminder();

        (ChatWithProspectResponse response, string newResponseId) = await _chat.SendChatMessageAsync(
            userInput: enhancedInput,
            systemPrompt: systemPrompt,
            previousResponseId: previousId,
            useWebSearch: command.UseWebSearch,
            temperature: command.Temperature,
            maxOutputTokens: command.MaxOutputTokens,
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

        if (!string.IsNullOrWhiteSpace(dto.SummarizedContext))
        {
            sb.AppendLine($"SUMMARY CONTEXT: {dto.SummarizedContext}");
        }

        if (dto.EnrichedData != null)
        {
            var ed = dto.EnrichedData;
            sb.AppendLine($"SNAPSHOT: {ed.Snapshot.WhatTheyDo}. They operate by {ed.Snapshot.HowTheyOperate}. Target: {ed.Snapshot.TargetCustomer}.");
            sb.AppendLine("PROFILE:");
            sb.AppendLine($"- Business Model: {ed.Profile.BusinessModel}");
            sb.AppendLine($"- Customer Type: {ed.Profile.CustomerType}");
            sb.AppendLine($"- Tech Posture: {ed.Profile.TechnologyPosture}");
            sb.AppendLine($"- Scaling Stage: {ed.Profile.ScalingStage}");

            if (ed.Challenges.Confirmed.Any() || ed.Challenges.Inferred.Any())
            {
                sb.AppendLine("CHALLENGES:");
                foreach (var c in ed.Challenges.Confirmed)
                    sb.AppendLine($"- [CONFIRMED] {c.ChallengeDescription} (Source: {c.SourceUrl})");
                foreach (var c in ed.Challenges.Inferred)
                    sb.AppendLine($"- [INFERRED] {c.ChallengeDescription} (Reason: {c.Reasoning})");
            }

            if (ed.OutreachHooks.Any())
            {
                sb.AppendLine("RECENT EVENTS/HOOKS:");
                foreach (var h in ed.OutreachHooks)
                    sb.AppendLine($"- {h.Date}: {h.HookDescription} (Why: {h.WhyItMatters})");
            }
        }

        return sb.ToString();
    }
}
