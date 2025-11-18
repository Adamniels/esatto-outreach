using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Esatto.Outreach.Application.Abstractions;
using Esatto.Outreach.Application.DTOs;
using Esatto.Outreach.Infrastructure.Common;
using Microsoft.Extensions.Options;

namespace Esatto.Outreach.Infrastructure.SoftDataCollection;

/// <summary>
/// Claude/Anthropic implementation of research service using Messages API.
/// </summary>
public sealed class ClaudeResearchService : IResearchService
{
    private readonly HttpClient _http;
    private readonly ClaudeOptions _options;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public ClaudeResearchService(HttpClient http, IOptions<ClaudeOptions> options)
    {
        _http = http;
        _http.BaseAddress = new Uri("https://api.anthropic.com/");
        _options = options.Value;
    }

    public async Task<SoftCompanyDataDto> GenerateCompanyResearchAsync(
        string companyName,
        string? domain,
        CancellationToken ct = default)
    {
        var systemPrompt = BuildResearchSystemPrompt();
        var userPrompt = BuildResearchUserPrompt(companyName, domain);

        var payload = BuildPayload(
            model: _options.Model,
            systemPrompt: systemPrompt,
            userPrompt: userPrompt,
            maxTokens: _options.MaxTokens
        );

        var outputText = await CallClaude(_options.ApiKey, payload, ct);

        return ParseResearchResponse(outputText, companyName);
    }

    private static string BuildResearchSystemPrompt()
    {
        return @"Du är en AI-assistent specialiserad på B2B-företagsresearch för säljoutreach.

Din uppgift är att hitta aktuella, relevanta insikter (""mjuk data"") om företag som kan användas som personaliserings-hooks i outreach-mejl.

Fokusera på:
- Nya nyheter, pressmeddelanden, produktlanseringar (senaste 6 månaderna)
- Event, webbinarier, konferenser (senaste 3 månaderna)
- Utmärkelser, certifieringar, milstolpar
- Hållbarhetsinitiativ, digital transformation, innovation
- Viktiga partnerskap, expansioner, nyanställningar
- Aktivitet på sociala medier (LinkedIn, Twitter)

VIKTIGT:
- Använd web_search och web_fetch tools för att samla information
- FABRICERA ALDRIG information – inkludera endast verifierade fakta från riktiga källor
- Prioritera information från de senaste 12 månaderna
- Inkludera käll-URL:er för all information
- All text i JSON-utdata MÅSTE vara på svenska

Returnera dina fynd som ett JSON-objekt med EXAKT denna struktur (med svensk text):

{
  ""hooksJson"": ""[{\""text\"":\""Beskrivning av hook (på svenska)\"",\""source\"":\""https://...\"",\""date\"":\""YYYY-MM-DD\"",\""relevance\"":\""high/medium/low\""}]"",
  ""recentEventsJson"": ""[{\""title\"":\""Eventnamn (på svenska)\"",\""date\"":\""YYYY-MM-DD\"",\""type\"":\""webinar/konferens/...\"",\""url\"":\""https://...\""}]"",
  ""newsItemsJson"": ""[{\""headline\"":\""Nyhetsrubrik (på svenska)\"",\""date\"":\""YYYY-MM-DD\"",\""source\"":\""Källans namn\"",\""url\"":\""https://...\""}]"",
  ""socialActivityJson"": ""[{\""platform\"":\""LinkedIn/Twitter\"",\""text\"":\""Inläggstext (på svenska)\"",\""date\"":\""YYYY-MM-DD\"",\""url\"":\""https://...\""}]"",
  ""sourcesJson"": ""[\""https://url1\"",\""https://url2\""]""
}

Notera: Alla JSON-fält måste vara JSON-escapade strängar. Använd null om ingen data hittas. All beskrivande text ska vara på svenska.";
    }

    private static string BuildResearchUserPrompt(string companyName, string? domain)
    {
        var prompt = new StringBuilder();
        prompt.AppendLine($"Researcha företaget: {companyName}");

        if (!string.IsNullOrWhiteSpace(domain))
        {
            prompt.AppendLine($"Domän: {domain}");
        }

        prompt.AppendLine();
        prompt.AppendLine("Använd web_search för att hitta aktuell information om företaget.");
        prompt.AppendLine("Använd sedan web_fetch för att hämta detaljerat innehåll från relevanta källor.");
        prompt.AppendLine("Fokusera på information från de senaste 12 månaderna som kan användas som personaliserings-hooks i B2B-säljmejl.");
        prompt.AppendLine();
        prompt.AppendLine("Returnera resultatet som JSON enligt det angivna formatet.");
        prompt.AppendLine("VIKTIGT: All text i JSON-objektet ska vara på svenska.");

        return prompt.ToString();
    }

    private static object BuildPayload(
        string model,
        string systemPrompt,
        string userPrompt,
        int maxTokens)
    {
        return new Dictionary<string, object?>
        {
            ["model"] = model,
            ["max_tokens"] = maxTokens,
            ["system"] = systemPrompt,
            ["messages"] = new object[]
            {
                new Dictionary<string, object>
                {
                    ["role"] = "user",
                    ["content"] = userPrompt
                }
            },
            ["tools"] = new object[]
            {
                new Dictionary<string, object>
                {
                    ["type"] = "web_search_20250305",
                    ["name"] = "web_search",
                    ["max_uses"] = 10
                },
                new Dictionary<string, object>
                {
                    ["type"] = "web_fetch_20250910",
                    ["name"] = "web_fetch",
                    ["max_uses"] = 10,
                    ["citations"] = new Dictionary<string, object>
                    {
                        ["enabled"] = true
                    }
                }
            }
        };
    }

    private async Task<string> CallClaude(string apiKey, object payload, CancellationToken ct)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, "v1/messages");
        req.Headers.Add("x-api-key", apiKey);
        req.Headers.Add("anthropic-version", "2023-06-01");
        req.Headers.Add("anthropic-beta", "web-fetch-2025-09-10");
        req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var reqBody = JsonSerializer.Serialize(payload, JsonOpts);
        req.Content = new StringContent(reqBody, Encoding.UTF8, "application/json");

        using var resp = await _http.SendAsync(req, ct);
        var body = await resp.Content.ReadAsStringAsync(ct);

        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException($"Claude HTTP {(int)resp.StatusCode}: {body}");

        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        return ExtractClaudeText(root);
    }

    private static string ExtractClaudeText(JsonElement root)
    {
        var sb = new StringBuilder();

        if (root.TryGetProperty("content", out var contentArr) && contentArr.ValueKind == JsonValueKind.Array)
        {
            foreach (var block in contentArr.EnumerateArray())
            {
                if (block.TryGetProperty("type", out var typeElem) &&
                    typeElem.GetString() == "text" &&
                    block.TryGetProperty("text", out var textElem))
                {
                    var text = textElem.GetString();
                    if (!string.IsNullOrEmpty(text))
                    {
                        sb.AppendLine(text);
                    }
                }
            }
        }

        return sb.ToString().Trim();
    }

    private static SoftCompanyDataDto ParseResearchResponse(string outputText, string companyName)
    {
        if (string.IsNullOrWhiteSpace(outputText))
        {
            throw new InvalidOperationException($"Claude returnerade tomt svar för {companyName}");
        }

        var cleanText = outputText.Trim();

        // Ta bort markdown code fences
        if (cleanText.StartsWith("```json"))
        {
            cleanText = cleanText.Substring(7);
        }
        if (cleanText.StartsWith("```"))
        {
            cleanText = cleanText.Substring(3);
        }
        if (cleanText.EndsWith("```"))
        {
            cleanText = cleanText.Substring(0, cleanText.Length - 3);
        }
        cleanText = cleanText.Trim();

        // Hitta JSON-objektet i texten
        var jsonStart = cleanText.IndexOf('{');
        var jsonEnd = cleanText.LastIndexOf('}');

        if (jsonStart >= 0 && jsonEnd > jsonStart)
        {
            cleanText = cleanText.Substring(jsonStart, jsonEnd - jsonStart + 1);
        }
        else if (jsonStart < 0)
        {
            throw new InvalidOperationException(
                $"Kunde inte hitta JSON i Claude-svar för {companyName}. Text: {cleanText.Substring(0, Math.Min(500, cleanText.Length))}");
        }

        try
        {
            var researchData = JsonSerializer.Deserialize<ResearchResponseData>(cleanText, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                AllowTrailingCommas = true,
                ReadCommentHandling = JsonCommentHandling.Skip
            });

            if (researchData == null)
            {
                throw new JsonException("Deserialisering returnerade null");
            }

            string? NormalizeJson(string? json) =>
                string.IsNullOrWhiteSpace(json) || json == "null" ? null : json;

            return new SoftCompanyDataDto(
                Id: Guid.Empty,
                ProspectId: Guid.Empty,
                HooksJson: NormalizeJson(researchData.HooksJson),
                RecentEventsJson: NormalizeJson(researchData.RecentEventsJson),
                NewsItemsJson: NormalizeJson(researchData.NewsItemsJson),
                SocialActivityJson: NormalizeJson(researchData.SocialActivityJson),
                SourcesJson: NormalizeJson(researchData.SourcesJson),
                ResearchedAt: DateTime.UtcNow,
                CreatedUtc: DateTime.UtcNow,
                UpdatedUtc: null
            );
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException(
                $"Kunde inte parsa Claude-svar som JSON för {companyName}. Rå text: {cleanText.Substring(0, Math.Min(500, cleanText.Length))}",
                ex);
        }
    }

    private sealed record ResearchResponseData(
        string? HooksJson,
        string? RecentEventsJson,
        string? NewsItemsJson,
        string? SocialActivityJson,
        string? SourcesJson
    );
}
