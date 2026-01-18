using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Esatto.Outreach.Application.Abstractions;
using Esatto.Outreach.Application.DTOs;
using Esatto.Outreach.Infrastructure.Common;
using Esatto.Outreach.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Esatto.Outreach.Infrastructure.EmailGeneration;

public sealed class OpenAICustomEmailGenerator : ICustomEmailGenerator
{
    private readonly HttpClient _http;
    private readonly OpenAiOptions _options;
    private readonly ILogger<OpenAICustomEmailGenerator> _logger;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public OpenAICustomEmailGenerator(
        HttpClient http,
        IOptions<OpenAiOptions> options,
        ILogger<OpenAICustomEmailGenerator> logger)
    {
        _http = http;
        _http.BaseAddress = new Uri("https://api.openai.com/");
        _options = options.Value;
        _logger = logger;
    }

    public async Task<CustomEmailDraftDto> GenerateAsync(
        EmailGenerationContext context,
        CancellationToken cancellationToken = default)
    {
        // 1. Bygg upp själva prompten från context
        var userPrompt = BuildPrompt(context) + @"

Return ONLY a valid JSON object with the following structure, nothing else:
{
  ""Title"": ""string"",
  ""BodyPlain"": ""string"",
  ""BodyHTML"": ""string""
}
Do not include code fences, explanations, or any extra text.
";

        // 3. Bygg payload för OpenAI Responses API
        var payload = BuildResponsesPayload(userPrompt);

        // 4. Kör request mot OpenAI Responses API
        var jsonText = await CallOpenAIAsync(payload, cancellationToken);

        // 5. Försök deserialisera till din DTO
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

        // 6. Säkerställ titel om den saknas
        if (string.IsNullOrWhiteSpace(dto.Title))
        {
            dto = dto with { Title = $"Introduktion till {context.Request.Name}".Trim() };
        }

        return dto;
    }

    private object BuildResponsesPayload(string userPrompt)
    {
        var payload = new Dictionary<string, object>
        {
            ["model"] = _options.Model,
            ["input"] = userPrompt
        };

        // Lägg till web search om aktiverat
        if (_options.UseWebSearch)
        {
            payload["tools"] = new object[]
            {
                new { type = "web_search_preview" }
            };
        }

        return payload;
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

    private static string BuildPrompt(EmailGenerationContext context)
    {
        var req = context.Request;

        // Statisk systemkontext (hårdkodad)
        var systemContext = @$"
            Du är en säljare på Esatto AB och ska skriva ett kort, personligt säljmejl på svenska (max 500 ord).
            
            === INFORMATION OM ESATTO AB ===
            {context.CompanyInfo}
            
            === MÅLFÖRETAG ===
            Företag: {req.Name}
            {(string.IsNullOrWhiteSpace(req.About) ? "" : $"Om företaget: {req.About}")}
            {(req.Websites?.Any() == true ? $"Webbplatser: {string.Join(", ", req.Websites)}" : "")}
            {(req.Addresses?.Any() == true ? $"Adresser: {string.Join("; ", req.Addresses)}" : "")}
            {(req.Tags?.Any() == true ? $"Taggar: {string.Join(", ", req.Tags)}" : "")}
            {(string.IsNullOrWhiteSpace(req.Notes) ? "" : $"Anteckningar: {req.Notes}")}";

        // Dynamiska instruktioner från databasen
        return systemContext + @$"

            === INSTRUKTIONER ===
            {context.Instructions}";
    }
}
// TIDIGARE VERSION AV PROMPTEN:
// === INSTRUKTIONER ===
//             Fokusera på hur vi (Esatto AB) kan hjälpa målföretaget. 
//             Använd informationen ovan om Esatto för att:
//             - Hitta relevanta cases som liknar kundens bransch eller utmaningar
//             - Visa konkret förståelse för kundens behov genom att referera till liknande projekt
//             - Matcha rätt tjänster och metoder till kundens situation
//             - Skriv i Esattos ton och värderingar (ärlighet, engagemang, omtanke, samarbete)

//             Krav:
//             - Hook i första meningen.
//             - 1–2 konkreta värdeförslag anpassade till företaget.
//             - Referera gärna till ett eller två relevant Esatto-case som exempel
//             - Avsluta med en enkel call-to-action (t.ex. 'Vill du att jag skickar ett konkret förslag?').";
