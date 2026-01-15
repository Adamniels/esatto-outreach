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
        var intelligence = context.EntityIntelligence!; 

        string collectedDataSection;

        // Try to parse rich data
        EnrichedCompanyDataDto? richData = null;
        if (!string.IsNullOrWhiteSpace(intelligence.CompanyHooksJson) && !intelligence.CompanyHooksJson.TrimStart().StartsWith("["))
        {
             try 
             {
                 richData = JsonSerializer.Deserialize<EnrichedCompanyDataDto>(intelligence.CompanyHooksJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
             }
             catch { /* Ignore, treat as legacy */ }
        }

        if (richData != null)
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== INSAMLAD DATA OM MÅLFÖRETAGET (RICH DATA) ===");
            
            sb.AppendLine($"\nSUMMARY: {richData.Summary}");
            
            if (richData.KeyValueProps?.Any() == true)
                sb.AppendLine($"VALUE PROPS: {string.Join(", ", richData.KeyValueProps)}");
                
            if (richData.TechStack?.Any() == true)
                sb.AppendLine($"TECH STACK: {string.Join(", ", richData.TechStack)}");
                
            if (richData.CaseStudies?.Any() == true)
            {
                sb.AppendLine("\nCASE STUDIES:");
                foreach(var c in richData.CaseStudies)
                {
                   sb.AppendLine($"- Client: {c.Client} | Challenge: {c.Challenge} | Solution: {c.Solution} | Outcome: {c.Outcome}");
                }
            }
            
            if (richData.News?.Any() == true)
            {
                sb.AppendLine("\nRECENT NEWS:");
                foreach(var n in richData.News) sb.AppendLine($"- {n.Date}: {n.Description}");
            }
            
            if (richData.Hiring?.Any() == true)
            {
                sb.AppendLine("\nHIRING:");
                foreach(var h in richData.Hiring) sb.AppendLine($"- {h.Role} ({h.Date})");
            }
            
             // Lägg till när datan samlades in
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

            if (!string.IsNullOrWhiteSpace(intelligence.CompanyHooksJson))
            {
                sb.AppendLine("\nFöretagskrokar (Company Hooks):");
                sb.AppendLine(intelligence.CompanyHooksJson);
            }
            
            if (!string.IsNullOrWhiteSpace(intelligence.PersonalHooksJson))
            {
                sb.AppendLine("\nPersonliga krokar (Personal Hooks):");
                sb.AppendLine(intelligence.PersonalHooksJson);
            }
             // Lägg till när datan samlades in
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
{(string.IsNullOrWhiteSpace(req.About) ? "" : $"Om företaget: {req.About}\n")}{(req.Websites?.Any() == true ? $"Webbplatser: {string.Join(", ", req.Websites)}\n" : "")}{(req.EmailAddresses?.Any() == true ? $"E-postadresser: {string.Join(", ", req.EmailAddresses)}\n" : "")}{(req.PhoneNumbers?.Any() == true ? $"Telefonnummer: {string.Join(", ", req.PhoneNumbers)}\n" : "")}{(req.Addresses?.Any() == true ? $"Adresser: {string.Join("; ", req.Addresses)}\n" : "")}{(req.Tags?.Any() == true ? $"Taggar: {string.Join(", ", req.Tags)}\n" : "")}{(string.IsNullOrWhiteSpace(req.Notes) ? "" : $"Anteckningar: {req.Notes}\n")}
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

