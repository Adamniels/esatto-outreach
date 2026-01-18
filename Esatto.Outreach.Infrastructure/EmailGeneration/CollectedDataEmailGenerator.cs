using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Esatto.Outreach.Application.Abstractions;
using Esatto.Outreach.Application.DTOs;
using Esatto.Outreach.Infrastructure.Common;

namespace Esatto.Outreach.Infrastructure.EmailGeneration;

public sealed class CollectedDataEmailGenerator : ICustomEmailGenerator
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

    public async Task<CustomEmailDraftDto> GenerateAsync(
        EmailGenerationContext context,
        CancellationToken cancellationToken = default)
    {
        // 1. Validera att soft data finns i context
        if (context.EntityIntelligence == null)
            throw new InvalidOperationException("No Entity Intelligence available in context. This generator requires it.");

        // 2. Bygg upp prompten med samlad data
        var userPrompt = BuildPromptWithCollectedData(context) + @"

Return ONLY a valid JSON object with the following structure, nothing else:
{
  ""Title"": ""string"",
  ""BodyPlain"": ""string"",
  ""BodyHTML"": ""string""
}
Do not include code fences, explanations, or any extra text.
";

        // 5. Bygg payload för OpenAI Responses API (UTAN web_search verktyg)
        var payload = BuildResponsesPayload(userPrompt);

        // 6. Kör request mot OpenAI Responses API
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

        // 7. Försök deserialisera till din DTO
        CustomEmailDraftDto? dto = null;
        try
        {
            dto = JsonSerializer.Deserialize<CustomEmailDraftDto>(jsonText, new JsonSerializerOptions
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

        // 8. Säkerställ titel om den saknas
        if (string.IsNullOrWhiteSpace(dto.Title))
        {
            dto = dto with { Title = $"Introduktion till {context.Request.Name}".Trim() };
        }

        return dto;
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

    private static string BuildPromptWithCollectedData(EmailGenerationContext context)
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
                foreach(var c in ed.Challenges.Confirmed)
                    sb.AppendLine($"- [CONFIRMED] {c.ChallengeDescription}");
                foreach(var c in ed.Challenges.Inferred)
                    sb.AppendLine($"- [INFERRED] {c.ChallengeDescription}");
            }
            
            if (ed.OutreachHooks.Any())
            {
                sb.AppendLine("\nRECENT EVENTS / HOOKS:");
                foreach(var h in ed.OutreachHooks)
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

        // Statisk systemkontext
        var systemContext = @$"
Du är en säljare på Esatto AB och ska skriva ett kort, personligt säljmejl på svenska (max 500 ord).

=== INFORMATION OM ESATTO AB ===
{context.CompanyInfo}

=== MÅLFÖRETAG ===
Företag: {req.Name}
{(string.IsNullOrWhiteSpace(req.About) ? "" : $"Om företaget: {req.About}\n")}{(req.Websites?.Any() == true ? $"Webbplatser: {string.Join(", ", req.Websites)}\n" : "")}{(req.Addresses?.Any() == true ? $"Adresser: {string.Join("; ", req.Addresses)}\n" : "")}{(req.Tags?.Any() == true ? $"Taggar: {string.Join(", ", req.Tags)}\n" : "")}{(string.IsNullOrWhiteSpace(req.Notes) ? "" : $"Anteckningar: {req.Notes}\n")}
{collectedDataSection}";

        // Dynamiska instruktioner från databasen
        return systemContext + @$"

=== INSTRUKTIONER ===
{context.Instructions}

VIKTIGT:
1. Använd informationen under 'INSAMLAD DATA' för att hitta en konkret koppling till kunden.
2. Matcha kundens utmaningar eller bransch (från Case Studies/Summary) med Esattos tjänster.
3. Skriv personligt och engagerande.
4. Referera gärna till liknande case om det finns i datan.";
    }
}

