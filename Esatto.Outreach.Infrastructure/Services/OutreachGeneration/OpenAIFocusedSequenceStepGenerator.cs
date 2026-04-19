using Esatto.Outreach.Application.Abstractions.Services;
using Esatto.Outreach.Application.Features.Intelligence.Shared;
using Esatto.Outreach.Application.Features.OutreachGeneration.Shared;
using Esatto.Outreach.Domain.Enums;
using Esatto.Outreach.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Esatto.Outreach.Infrastructure.Services.OutreachGeneration;

public sealed class OpenAIFocusedSequenceStepGenerator : IFocusedSequenceStepGenerator
{
    private readonly HttpClient _http;
    private readonly OpenAiOptions _options;
    private readonly ILogger<OpenAIFocusedSequenceStepGenerator> _logger;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public OpenAIFocusedSequenceStepGenerator(
        HttpClient http,
        IOptions<OpenAiOptions> options,
        ILogger<OpenAIFocusedSequenceStepGenerator> logger)
    {
        _http = http;
        _http.BaseAddress = new Uri("https://api.openai.com/");
        _options = options.Value;
        _logger = logger;
    }

    public async Task<CustomOutreachDraftDto> GenerateAsync(
        FocusedSequenceStepContext context,
        CancellationToken ct = default)
    {
        var messages = BuildMessageArray(context);
        var payload = BuildPayload(messages, context.Channel);

        var jsonText = await CallOpenAIAsync(payload, ct);

        CustomOutreachDraftDto? dto;
        try
        {
            dto = JsonSerializer.Deserialize<CustomOutreachDraftDto>(jsonText, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Failed to parse model JSON output: {jsonText}", ex);
        }

        if (dto == null)
            throw new InvalidOperationException($"Model returned null or invalid JSON: {jsonText}");

        if (string.IsNullOrWhiteSpace(dto.Title))
            dto = dto with { Title = $"Uppföljning — {context.Prospect.Name}".Trim() };

        return dto with { Channel = context.Channel };
    }

    // Builds the full multi-turn message array.
    // Step 1: one user message with full system context + step 1 instruction.
    // Step N: full reconstructed conversation from prior turns, ending with step N instruction.
    private IReadOnlyList<object> BuildMessageArray(FocusedSequenceStepContext context)
    {
        var messages = new List<object>();
        var systemContext = BuildSystemContext(context);

        // Determine what step 1 looks like
        var step1Type = context.PriorTurns.Count > 0 ? context.PriorTurns[0].StepType : context.StepType;
        var step1Instruction = $"Detta är en {context.TotalSteps}-stegs outreach-sekvens. Skriv steg 1 — ett {FormatStepType(step1Type)} som introduktion. Tänk på att det kommer {context.TotalSteps - 1} uppföljningssteg efter detta.";

        var jsonInstruction = BuildJsonInstruction(context.Channel);

        if (context.PriorTurns.Count == 0)
        {
            // Current step IS step 1 — single message, append JSON format instruction
            messages.Add(new { role = "user", content = $"{systemContext}\n\n{step1Instruction}\n\n{jsonInstruction}" });
            return messages;
        }

        // Step 1 instruction (no JSON instruction here — only on the final user message)
        messages.Add(new { role = "user", content = $"{systemContext}\n\n{step1Instruction}" });

        // Interleave assistant turns + follow-up user prompts
        for (int i = 0; i < context.PriorTurns.Count; i++)
        {
            var turn = context.PriorTurns[i];

            // What was previously generated for this step
            var assistantContent = turn.StepType == SequenceStepType.Email && !string.IsNullOrWhiteSpace(turn.Subject)
                ? $"Ämne: {turn.Subject}\n\n{turn.Body}"
                : turn.Body;

            messages.Add(new { role = "assistant", content = assistantContent });

            // Instruction for the next step
            bool isLastPriorTurn = (i == context.PriorTurns.Count - 1);
            int nextStepNumber = turn.StepNumber + 1;
            SequenceStepType nextStepType = isLastPriorTurn ? context.StepType : context.PriorTurns[i + 1].StepType;
            int delayOfNextStep = isLastPriorTurn ? context.DelayInDays : context.PriorTurns[i + 1].DelayInDays;
            bool isFinalStep = nextStepNumber == context.TotalSteps;

            var followUpInstruction = BuildFollowUpInstruction(nextStepNumber, context.TotalSteps, nextStepType, delayOfNextStep, isFinalStep);

            // Append JSON format instruction only to the last user message
            var userMessage = isLastPriorTurn
                ? $"{followUpInstruction}\n\n{jsonInstruction}"
                : followUpInstruction;

            messages.Add(new { role = "user", content = userMessage });
        }

        return messages;
    }

    private static string BuildFollowUpInstruction(int stepNumber, int totalSteps, SequenceStepType stepType, int delayInDays, bool isFinalStep)
    {
        var delayText = delayInDays <= 0
            ? "Direkt efteråt"
            : $"{delayInDays} dag{(delayInDays == 1 ? "" : "ar")} har gått utan svar";

        var finalNote = isFinalStep
            ? " Det här är det sista steget i sekvensen — gör det tydligt och sätt en tydlig call-to-action."
            : string.Empty;

        return $"{delayText}. Skriv steg {stepNumber} av {totalSteps} — ett {FormatStepType(stepType)} som uppföljning. Upprepa inte vad som redan sagts; bygg vidare på det naturligt.{finalNote}";
    }

    private static string BuildSystemContext(FocusedSequenceStepContext context)
    {
        var req = context.Prospect;
        var projectCasesSection = FormatProjectCases(context.ProjectCases);

        var sb = new StringBuilder();
        sb.AppendLine($"Du är en säljare på {context.CompanyInfo.Name} och skriver en {context.TotalSteps}-stegs outreach-sekvens på svenska (max 300 ord per meddelande).");
        sb.AppendLine();
        sb.AppendLine($"=== OM OSS ({context.CompanyInfo.Name}) ===");
        sb.AppendLine(context.CompanyInfo.Overview);
        sb.AppendLine(context.CompanyInfo.ValueProposition);
        sb.AppendLine();
        sb.AppendLine("=== CASE STUDIES ===");
        sb.AppendLine(projectCasesSection);
        sb.AppendLine();
        sb.AppendLine("=== MÅLFÖRETAG ===");
        sb.AppendLine($"Företag: {req.Name}");
        if (!string.IsNullOrWhiteSpace(req.About)) sb.AppendLine($"Om företaget: {req.About}");
        if (req.Websites?.Any() == true) sb.AppendLine($"Webbplatser: {string.Join(", ", req.Websites)}");
        if (req.Tags?.Any() == true) sb.AppendLine($"Taggar: {string.Join(", ", req.Tags)}");
        if (!string.IsNullOrWhiteSpace(req.Notes)) sb.AppendLine($"Anteckningar: {req.Notes}");

        // Include enriched intelligence if available (UseCollectedData mode)
        if (context.EntityIntelligence?.EnrichedData != null)
        {
            var ed = context.EntityIntelligence.EnrichedData;
            sb.AppendLine();
            sb.AppendLine("=== INSAMLAD INTEL ===");
            sb.AppendLine($"Snapshot: {ed.Snapshot.WhatTheyDo}. Verksamhet: {ed.Snapshot.HowTheyOperate}. Målkund: {ed.Snapshot.TargetCustomer}.");
            sb.AppendLine($"Affärsmodell: {ed.Profile.BusinessModel} | Kundtyp: {ed.Profile.CustomerType} | Teknik: {ed.Profile.TechnologyPosture}");

            if (ed.Challenges.Confirmed.Any())
                sb.AppendLine($"Bekräftade utmaningar: {string.Join("; ", ed.Challenges.Confirmed.Select(c => c.ChallengeDescription))}");
            if (ed.Challenges.Inferred.Any())
                sb.AppendLine($"Troliga utmaningar: {string.Join("; ", ed.Challenges.Inferred.Select(c => c.ChallengeDescription))}");
            if (ed.OutreachHooks.Any())
                sb.AppendLine($"Relevanta händelser: {string.Join("; ", ed.OutreachHooks.Select(h => $"{h.Date}: {h.HookDescription}"))}");
        }
        else if (context.EntityIntelligence?.SummarizedContext != null)
        {
            sb.AppendLine();
            sb.AppendLine("=== INSAMLAD INTEL ===");
            sb.AppendLine(context.EntityIntelligence.SummarizedContext);
        }

        if (context.ActiveContact != null)
        {
            sb.AppendLine();
            sb.AppendLine("=== KONTAKTPERSON ===");
            sb.AppendLine($"Namn: {context.ActiveContact.Name}");
            if (!string.IsNullOrWhiteSpace(context.ActiveContact.Title)) sb.AppendLine($"Titel: {context.ActiveContact.Title}");
            if (context.ActiveContact.PersonalHooks?.Any() == true) sb.AppendLine($"Personliga hooks: {string.Join(", ", context.ActiveContact.PersonalHooks)}");
            if (context.ActiveContact.PersonalNews?.Any() == true) sb.AppendLine($"Senaste nyheter: {string.Join(", ", context.ActiveContact.PersonalNews)}");
            if (!string.IsNullOrWhiteSpace(context.ActiveContact.Summary)) sb.AppendLine($"Sammanfattning: {context.ActiveContact.Summary}");
        }

        sb.AppendLine();
        sb.AppendLine("=== INSTRUKTIONER ===");
        sb.AppendLine(context.Instructions);
        sb.AppendLine();
        sb.AppendLine("VIKTIGT:");
        sb.AppendLine($"- Skriv på svenska.");
        sb.AppendLine($"- Fokusera på hur {context.CompanyInfo.Name} kan hjälpa målföretaget.");
        sb.AppendLine("- Håll varje meddelande kort och tydligt (max 300 ord).");
        sb.AppendLine("- Varje uppföljning ska ha en ny vinkel — upprepa inte tidigare meddelanden.");

        if (context.ActiveContact != null)
            sb.AppendLine($"- Tilltala kontaktpersonen med namn: {context.ActiveContact.Name}");
        else
            sb.AppendLine("- Ingen specifik kontaktperson — skriv generellt till företaget. Använd inte platshållare som [Namn].");

        if (!string.IsNullOrWhiteSpace(context.UserFullName))
            sb.AppendLine($"- Signera med: '{context.UserFullName}, {context.CompanyInfo.Name}'");

        return sb.ToString().TrimEnd();
    }

    private static string BuildJsonInstruction(OutreachChannel channel)
    {
        var format = channel == OutreachChannel.Email
            ? @"{""Title"": ""string"", ""BodyPlain"": ""string"", ""BodyHTML"": ""string""}"
            : @"{""BodyPlain"": ""string""}";

        return $"Returnera ENBART ett giltigt JSON-objekt med följande struktur, inget annat:\n{format}\nIngen kodblock, förklaringar eller extra text.";
    }

    private static string FormatProjectCases(List<ProjectCaseDto>? projectCases)
    {
        if (projectCases is not { Count: > 0 })
            return "Inga tidigare projekt tillgängliga.";

        var activeCases = projectCases.Where(c => c.IsActive).ToList();
        if (activeCases.Count == 0)
            return "Inga aktiva projekt tillgängliga.";

        return string.Join("\n", activeCases.Select(c =>
        {
            var name = c.ClientName ?? "Okänt företag";
            var text = c.Text ?? "";
            return string.IsNullOrWhiteSpace(text) ? $"• {name}" : $"• {name}: {text}";
        }));
    }

    private static string FormatStepType(SequenceStepType stepType) => stepType switch
    {
        SequenceStepType.Email => "e-postmeddelande",
        SequenceStepType.LinkedInMessage => "LinkedIn-meddelande",
        SequenceStepType.LinkedInConnectionRequest => "LinkedIn-kontaktförfrågan",
        SequenceStepType.LinkedInInteraction => "LinkedIn-interaktion",
        _ => "meddelande"
    };

    private object BuildPayload(IReadOnlyList<object> messages, OutreachChannel channel)
    {
        return new Dictionary<string, object>
        {
            ["model"] = _options.Model,
            ["input"] = messages,
            ["max_output_tokens"] = _options.DefaultMaxOutputTokens > 0 ? _options.DefaultMaxOutputTokens : 2000
        };
    }

    private async Task<string> CallOpenAIAsync(object payload, CancellationToken ct)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, "v1/responses");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
        req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var reqBody = JsonSerializer.Serialize(payload, JsonOpts);
        req.Content = new StringContent(reqBody, Encoding.UTF8, "application/json");

        using var resp = await _http.SendAsync(req, ct);
        var body = await resp.Content.ReadAsStringAsync(ct);

        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException($"OpenAI HTTP {(int)resp.StatusCode}: {body}");

        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        if (root.TryGetProperty("output", out var output) &&
            output.ValueKind == JsonValueKind.Array &&
            output.GetArrayLength() > 0)
        {
            var firstOutput = output[0];
            if (firstOutput.TryGetProperty("content", out var contentArray) &&
                contentArray.ValueKind == JsonValueKind.Array &&
                contentArray.GetArrayLength() > 0)
            {
                var firstContent = contentArray[0];
                if (firstContent.TryGetProperty("text", out var text))
                    return text.GetString()?.Trim() ?? string.Empty;
            }
        }

        throw new InvalidOperationException("Could not extract content from OpenAI Responses API response");
    }
}
