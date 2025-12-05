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
        if (context.SoftData == null)
            throw new InvalidOperationException("No collected soft data available in context. This generator requires soft data.");

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
        // TODO: inte säker på att jag vill göra detta, om titel inte finns så vill jag nog få error?
        // samtidigt kan man bara ändra det
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
            ["input"] = userPrompt
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
        var softData = context.SoftData!; // Safe to use ! because we validated it's not null

        // Bygg upp collected data-sektionen
        var collectedDataSection = new StringBuilder();
        collectedDataSection.AppendLine("=== INSAMLAD DATA OM MÅLFÖRETAGET ===");

        if (!string.IsNullOrWhiteSpace(softData.HooksJson))
        {
            collectedDataSection.AppendLine("\nPersonaliseringshoooks:");
            collectedDataSection.AppendLine(softData.HooksJson);
        }

        if (!string.IsNullOrWhiteSpace(softData.RecentEventsJson))
        {
            collectedDataSection.AppendLine("\nSenaste händelser:");
            collectedDataSection.AppendLine(softData.RecentEventsJson);
        }

        if (!string.IsNullOrWhiteSpace(softData.NewsItemsJson))
        {
            collectedDataSection.AppendLine("\nNyheter:");
            collectedDataSection.AppendLine(softData.NewsItemsJson);
        }

        if (!string.IsNullOrWhiteSpace(softData.SocialActivityJson))
        {
            collectedDataSection.AppendLine("\nSocial aktivitet:");
            collectedDataSection.AppendLine(softData.SocialActivityJson);
        }

        // Lägg till när datan samlades in
        collectedDataSection.AppendLine($"\n(Data insamlad: {softData.ResearchedAt:yyyy-MM-dd})");

        // Statisk systemkontext
        var systemContext = @$"
Du är en säljare på Esatto AB och ska skriva ett kort, personligt säljmejl på svenska (max 500 ord).

=== INFORMATION OM ESATTO AB ===
{context.CompanyInfo}

=== MÅLFÖRETAG ===
Företag: {req.Name}
Webbplats: {req.Website}
E-post: {req.Email}
LinkedIn: {req.LinkedInUrl}
Anteckningar: {req.Notes}

{collectedDataSection}";

        // Dynamiska instruktioner från databasen
        return systemContext + @$"

=== INSTRUKTIONER ===
{context.Instructions}

VIKTIGT: Använd den insamlade datan ovan för att skapa ett personligt och relevant mejl. 
Referera till specifika händelser, nyheter eller aktiviteter som är aktuella för företaget.";
    }
}
