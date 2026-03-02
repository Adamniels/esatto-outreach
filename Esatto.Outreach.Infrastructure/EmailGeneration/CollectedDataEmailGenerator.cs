using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Esatto.Outreach.Application.Abstractions;
using Esatto.Outreach.Application.DTOs;
using Esatto.Outreach.Infrastructure.Common;
using Esatto.Outreach.Domain.Enums;

namespace Esatto.Outreach.Infrastructure.EmailGeneration;

// TODO: change name should be CollectedDataOutreachGenerator 
public sealed class CollectedDataEmailGenerator : IOutreachGenerator
{
    private readonly HttpClient _http;
    private readonly OpenAiOptions _options;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public CollectedDataEmailGenerator(
        HttpClient http,
        IOptions<OpenAiOptions> options)
    {
        _http = http;
        _http.BaseAddress = new Uri("https://api.openai.com/");
        _options = options.Value;
    }

    public async Task<CustomOutreachDraftDto> GenerateAsync(
        OutreachGenerationContext context,
        CancellationToken cancellationToken = default)
    {
        // 1. Validate that enrichment is collected
        if (context.EntityIntelligence == null)
            throw new InvalidOperationException("No Entity Intelligence available in context. This generator requires it.");

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

        // 2. Build the prompt with the collected data
        var userPrompt = BuildPromptWithCollectedData(context) + $@"

Return ONLY a valid JSON object with the following structure, nothing else:{jsonFormatSpecifier}
Do not include code fences, explanations, or any extra text.
";

        // 3. Build payload for OpenAI Responses API (wo websearch)
        var payload = BuildResponsesPayload(userPrompt);

        // 4. Run request
        var jsonText = await CallOpenAIAsync(payload, cancellationToken);

        // Sanitize JSON (remove markdown code fences if present)
        if (jsonText.StartsWith("```json"))
        {
            jsonText = jsonText.Substring(7);
            if (jsonText.EndsWith("```")) jsonText = jsonText.Substring(0, jsonText.Length - 3);
        }
        else if (jsonText.StartsWith("```"))
        {
            jsonText = jsonText.Substring(3);
            if (jsonText.EndsWith("```")) jsonText = jsonText.Substring(0, jsonText.Length - 3);
        }

        jsonText = jsonText.Trim();

        // 5. Try to Deserialize to dto
        CustomOutreachDraftDto? dto = null;
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

        // Incase title is missing, make a manual
        if (string.IsNullOrWhiteSpace(dto.Title))
        {
            dto = dto with { Title = $"Introduktion till {context.Request.Name}".Trim() };
        }

        return dto with { Channel = context.Channel };
    }

    private object BuildResponsesPayload(string userPrompt)
    {
        // Viktig skillnad: Vi använder INTE web_search verktyget här
        // eftersom vi redan har samlad data
        return new Dictionary<string, object>
        {
            ["model"] = _options.Model,
            ["input"] = userPrompt,
            ["max_output_tokens"] = _options.DefaultMaxOutputTokens > 0 ? _options.DefaultMaxOutputTokens : 2000
        };
    }

    private async Task<string> CallOpenAIAsync(object payload, CancellationToken cancellationToken)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, "v1/responses");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
        req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var reqBody = JsonSerializer.Serialize(payload, JsonOpts);
        req.Content = new StringContent(reqBody, Encoding.UTF8, "application/json");

        using var resp = await _http.SendAsync(req, cancellationToken);
        var body = await resp.Content.ReadAsStringAsync(cancellationToken);

        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException($"OpenAI HTTP {(int)resp.StatusCode}: {body}");

        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        // Extrahera text från Responses API format
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
                {
                    return text.GetString()?.Trim() ?? string.Empty;
                }
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

    private static string BuildPromptWithCollectedData(OutreachGenerationContext context)
    {
        var req = context.Request;
        var intelligence = context.EntityIntelligence!;

        string collectedDataSection;

        // Try to access rich data (New Structure)
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
                sb.AppendLine("\nCHALLENGES (PAIN POINTS):");
                foreach (var c in ed.Challenges.Confirmed)
                    sb.AppendLine($"- [CONFIRMED] {c.ChallengeDescription}");
                foreach (var c in ed.Challenges.Inferred)
                    sb.AppendLine($"- [INFERRED] {c.ChallengeDescription}");
            }

            if (ed.OutreachHooks.Any())
            {
                sb.AppendLine("\nRECENT EVENTS / HOOKS:");
                foreach (var h in ed.OutreachHooks)
                    sb.AppendLine($"- {h.Date}: {h.HookDescription} (Why: {h.WhyItMatters})");
            }

            sb.AppendLine($"\n(Data insamlad: {intelligence.ResearchedAt:yyyy-MM-dd})");
            collectedDataSection = sb.ToString();
        }
        else
        {
            // Legacy / Fallback
            var sb = new StringBuilder();
            sb.AppendLine("=== INSAMLAD DATA OM MÅLFÖRETAGET/PERSONEN ===");

            if (!string.IsNullOrWhiteSpace(intelligence.SummarizedContext))
            {
                sb.AppendLine("\nSammanfattning:");
                sb.AppendLine(intelligence.SummarizedContext);
            }

            sb.AppendLine($"\n(Data insamlad: {intelligence.ResearchedAt:yyyy-MM-dd})");
            collectedDataSection = sb.ToString();
        }


        var projectCasesSection = FormatProjectCases(context.ProjectCases);

        // Statisk systemkontext
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

        // Kontaktperson och signatur
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

        // Dynamiska instruktioner från databasen
        return systemContext + contactGreeting + @$"

=== INSTRUKTIONER ===
{context.Instructions}

VIKTIGT:
- Använd informationen under 'INSAMLAD DATA' för att hitta en konkret koppling till kunden.
- Matcha kundens utmaningar eller bransch (från Case Studies/Summary) med {context.CompanyInfo.Name} tjänster.
- Skriv personligt och engagerande.{(context.ActiveContact != null ? $"\n- Tilltala kontaktpersonen med namn: {context.ActiveContact.Name}" : "\n- Då ingen kontaktperson finns angiven: Skriv generellt till företaget. Använd INTE placeholders som [Namn]. Starta med 'Hej,' eller liknande.")}{signatureInstruction}{outputFormatInstruction}";
    }
}
