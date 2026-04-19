using Esatto.Outreach.Infrastructure.Options;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Esatto.Outreach.Application.Abstractions.Services;
using Esatto.Outreach.Application.Features.Intelligence.Shared;
using Esatto.Outreach.Application.Features.OutreachGeneration.Shared;
using Esatto.Outreach.Domain.Enums;

namespace Esatto.Outreach.Infrastructure.Services.OutreachGeneration;

public sealed class CollectedDataColdOutreachGenerator : IColdOutreachGenerator
{
    private readonly HttpClient _http;
    private readonly OpenAiOptions _options;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public CollectedDataColdOutreachGenerator(
        HttpClient http,
        IOptions<OpenAiOptions> options)
    {
        _http = http;
        _http.BaseAddress = new Uri("https://api.openai.com/");
        _options = options.Value;
    }

    public async Task<CustomOutreachDraftDto> GenerateAsync(
        ColdOutreachContext context,
        CancellationToken ct = default)
    {
        if (context.EntityIntelligence == null)
            throw new InvalidOperationException("No Entity Intelligence in context. This generator requires it.");

        var jsonFormatSpecifier = context.Channel == OutreachChannel.Email
            ? @"
{
  ""Title"": ""string"",
  ""BodyPlain"": ""string"",
  ""BodyHTML"": ""string""
}"
            : @"
{
  ""BodyPlain"": ""string""
}";

        var userPrompt = BuildPromptWithCollectedData(context) + $@"

Return ONLY a valid JSON object with the following structure, nothing else:{jsonFormatSpecifier}
Do not include code fences, explanations, or any extra text.
";

        var payload = BuildResponsesPayload(userPrompt);
        var jsonText = await CallOpenAIAsync(payload, ct);

        jsonText = SanitizeJson(jsonText);

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
            dto = dto with { Title = $"Introduktion till {context.Prospect.Name}".Trim() };

        return dto with { Channel = context.Channel };
    }

    private static string SanitizeJson(string jsonText)
    {
        if (jsonText.StartsWith("```json"))
            jsonText = jsonText[7..];
        else if (jsonText.StartsWith("```"))
            jsonText = jsonText[3..];

        if (jsonText.EndsWith("```"))
            jsonText = jsonText[..^3];

        return jsonText.Trim();
    }

    private object BuildResponsesPayload(string userPrompt)
    {
        return new Dictionary<string, object>
        {
            ["model"] = _options.Model,
            ["input"] = userPrompt,
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

    private static string FormatProjectCases(List<ProjectCaseDto>? projectCases)
    {
        if (projectCases is not { Count: > 0 })
            return "Inga tidigare projekt tillgängliga.";

        var activeCases = projectCases.Where(c => c.IsActive).ToList();
        if (activeCases.Count == 0)
            return "Inga aktiva projekt tillgängliga.";

        return string.Join("\n\n", activeCases.Select(c =>
        {
            var name = c.ClientName ?? "Okänt företag";
            var text = c.Text ?? "";
            return string.IsNullOrWhiteSpace(text) ? $"• {name}" : $"• {name}: {text}";
        }));
    }

    private static string BuildPromptWithCollectedData(ColdOutreachContext context)
    {
        var req = context.Prospect;
        var intelligence = context.EntityIntelligence!;

        string collectedDataSection;

        if (intelligence.EnrichedData != null)
        {
            var ed = intelligence.EnrichedData;
            var sb = new StringBuilder();
            sb.AppendLine("=== INSAMLAD DATA OM MÅLFÖRETAGET (RICH DATA) ===");
            sb.AppendLine($"\nSNAPSHOT: {ed.Snapshot.WhatTheyDo}. They operate by {ed.Snapshot.HowTheyOperate}. Target: {ed.Snapshot.TargetCustomer}.");
            sb.AppendLine("\nPROFILE:");
            sb.AppendLine($"- Business Model: {ed.Profile.BusinessModel}");
            sb.AppendLine($"- Customer Type: {ed.Profile.CustomerType}");
            sb.AppendLine($"- Tech Posture: {ed.Profile.TechnologyPosture}");
            sb.AppendLine($"- Scaling Stage: {ed.Profile.ScalingStage}");

            if (ed.Challenges.Confirmed.Any() || ed.Challenges.Inferred.Any())
            {
                sb.AppendLine("\nCHALLENGES:");
                foreach (var c in ed.Challenges.Confirmed)
                    sb.AppendLine($"- [CONFIRMED] {c.ChallengeDescription}");
                foreach (var c in ed.Challenges.Inferred)
                    sb.AppendLine($"- [INFERRED] {c.ChallengeDescription}");
            }

            if (ed.OutreachHooks.Any())
            {
                sb.AppendLine("\nRECENT HOOKS:");
                foreach (var h in ed.OutreachHooks)
                    sb.AppendLine($"- {h.Date}: {h.HookDescription} (Why: {h.WhyItMatters})");
            }

            sb.AppendLine($"\n(Data collected: {intelligence.ResearchedAt:yyyy-MM-dd})");
            collectedDataSection = sb.ToString();
        }
        else
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== INSAMLAD DATA OM MÅLFÖRETAGET ===");
            if (!string.IsNullOrWhiteSpace(intelligence.SummarizedContext))
                sb.AppendLine(intelligence.SummarizedContext);
            sb.AppendLine($"\n(Data collected: {intelligence.ResearchedAt:yyyy-MM-dd})");
            collectedDataSection = sb.ToString();
        }

        var projectCasesSection = FormatProjectCases(context.ProjectCases);
        string targetFormat = context.Channel == OutreachChannel.Email ? "sälj mejl" : "LinkedIn-meddelande";

        var systemContext = @$"
Du är en säljare på {context.CompanyInfo.Name} och ska skriva ett kort, personligt {targetFormat} på svenska (max 500 ord).

=== INFORMATION OM OSS ({context.CompanyInfo.Name}) ===
{context.CompanyInfo.Overview}

=== TIDIGARE PROJEKT/CASES ===
{projectCasesSection}
{context.CompanyInfo.ValueProposition}

=== MÅLFÖRETAG ===
Företag: {req.Name}
{(string.IsNullOrWhiteSpace(req.About) ? "" : $"Om företaget: {req.About}\n")}{(req.Websites?.Any() == true ? $"Webbplatser: {string.Join(", ", req.Websites)}\n" : "")}{(req.Tags?.Any() == true ? $"Taggar: {string.Join(", ", req.Tags)}\n" : "")}{(string.IsNullOrWhiteSpace(req.Notes) ? "" : $"Anteckningar: {req.Notes}\n")}<END>
{collectedDataSection}";

        var contactGreeting = context.ActiveContact != null
            ? $@"

=== KONTAKTPERSON ===
Namn: {context.ActiveContact.Name}
{(string.IsNullOrWhiteSpace(context.ActiveContact.Title) ? "" : $"Titel: {context.ActiveContact.Title}\n")}{(string.IsNullOrWhiteSpace(context.ActiveContact.Email) ? "" : $"E-post: {context.ActiveContact.Email}\n")}{(context.ActiveContact.PersonalHooks?.Any() == true ? $"Personliga hooks: {string.Join(", ", context.ActiveContact.PersonalHooks)}\n" : "")}{(context.ActiveContact.PersonalNews?.Any() == true ? $"Senaste nyheter: {string.Join(", ", context.ActiveContact.PersonalNews)}\n" : "")}{(string.IsNullOrWhiteSpace(context.ActiveContact.Summary) ? "" : $"Sammanfattning: {context.ActiveContact.Summary}\n")}"
            : "";

        var signatureInstruction = !string.IsNullOrWhiteSpace(context.UserFullName)
            ? $"\n- Avsluta meddelandet med din signatur: '{context.UserFullName}, {context.CompanyInfo.Name}'"
            : "";

        var outputFormatInstruction = context.Channel switch
        {
            OutreachChannel.Email => "\n- Skriv i ett format som är lätt att konvertera till ett mejl, med en tydlig ämnesrad och en personlig, engagerande brödtext.",
            OutreachChannel.LinkedIn => "\n- Skriv i ett format som passar för ett LinkedIn-meddelande, med en hook i början och en personlig, engagerande ton.",
            _ => ""
        };

        return systemContext + contactGreeting + @$"

=== INSTRUKTIONER ===
{context.Instructions}

VIKTIGT:
- Använd informationen under 'INSAMLAD DATA' för att hitta en konkret koppling till kunden.
- Matcha kundens utmaningar eller bransch (från Case Studies) med {context.CompanyInfo.Name} tjänster.
- Skriv personligt och engagerande.{(context.ActiveContact != null ? $"\n- Tilltala kontaktpersonen med namn: {context.ActiveContact.Name}" : "\n- Då ingen kontaktperson finns angiven: Skriv generellt till företaget. Använd INTE placeholders som [Namn]. Starta med 'Hej,' eller liknande.")}{signatureInstruction}{outputFormatInstruction}";
    }
}
