using Esatto.Outreach.Application.Features.Intelligence.Shared;
using Esatto.Outreach.Application.Features.OutreachGeneration.Shared;
using Esatto.Outreach.Domain.Enums;
using Esatto.Outreach.Infrastructure.Options;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Esatto.Outreach.Infrastructure.Services.OutreachGeneration;

public abstract class OpenAIOutreachGeneratorBase
{
    protected readonly HttpClient Http;
    protected readonly OpenAiOptions Options;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    protected OpenAIOutreachGeneratorBase(HttpClient http, IOptions<OpenAiOptions> options)
    {
        Http = http;
        Http.BaseAddress = new Uri("https://api.openai.com/");
        Options = options.Value;
    }

    protected async Task<string> CallWithStringInputAsync(string input, bool useWebSearch, CancellationToken ct)
    {
        var payload = new Dictionary<string, object>
        {
            ["model"] = Options.Model,
            ["input"] = input
        };

        if (useWebSearch)
            payload["tools"] = new object[] { new { type = "web_search_preview" } };
        else if (Options.DefaultMaxOutputTokens > 0)
            payload["max_output_tokens"] = Options.DefaultMaxOutputTokens;

        return await CallOpenAIAsync(payload, ct);
    }

    protected async Task<string> CallWithMessageArrayAsync(IReadOnlyList<object> messages, CancellationToken ct)
    {
        var payload = new Dictionary<string, object>
        {
            ["model"] = Options.Model,
            ["input"] = messages,
            ["max_output_tokens"] = Options.DefaultMaxOutputTokens > 0 ? Options.DefaultMaxOutputTokens : 2000
        };

        return await CallOpenAIAsync(payload, ct);
    }

    private async Task<string> CallOpenAIAsync(object payload, CancellationToken ct)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, "v1/responses");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Options.ApiKey);
        req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var body = JsonSerializer.Serialize(payload, JsonOpts);
        req.Content = new StringContent(body, Encoding.UTF8, "application/json");

        using var resp = await Http.SendAsync(req, ct);
        var respBody = await resp.Content.ReadAsStringAsync(ct);

        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException($"OpenAI HTTP {(int)resp.StatusCode}: {respBody}");

        using var doc = JsonDocument.Parse(respBody);
        var root = doc.RootElement;

        if (root.TryGetProperty("output", out var output) &&
            output.ValueKind == JsonValueKind.Array && output.GetArrayLength() > 0)
        {
            var first = output[0];
            if (first.TryGetProperty("content", out var content) &&
                content.ValueKind == JsonValueKind.Array && content.GetArrayLength() > 0)
            {
                var firstContent = content[0];
                if (firstContent.TryGetProperty("text", out var text))
                    return text.GetString()?.Trim() ?? string.Empty;
            }
        }

        throw new InvalidOperationException("Could not extract content from OpenAI Responses API response");
    }

    protected static CustomOutreachDraftDto ParseAndValidate(string jsonText, OutreachChannel channel, string fallbackName)
    {
        var sanitized = SanitizeJson(jsonText);

        CustomOutreachDraftDto? dto;
        try
        {
            dto = JsonSerializer.Deserialize<CustomOutreachDraftDto>(sanitized, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Failed to parse model JSON output: {sanitized}", ex);
        }

        if (dto == null)
            throw new InvalidOperationException($"Model returned null or invalid JSON: {sanitized}");

        if (string.IsNullOrWhiteSpace(dto.Title))
            dto = dto with { Title = fallbackName };

        return dto with { Channel = channel };
    }

    protected static string BuildJsonInstruction(OutreachChannel channel)
    {
        var format = channel == OutreachChannel.Email
            ? @"{""Title"": ""string"", ""BodyPlain"": ""string"", ""BodyHTML"": ""string""}"
            : @"{""BodyPlain"": ""string""}";

        return $"Returnera ENBART ett giltigt JSON-objekt med följande struktur, inget annat:\n{format}\nIngen kodblock, förklaringar eller extra text.";
    }

    protected static string FormatProjectCases(List<ProjectCaseDto>? projectCases)
    {
        if (projectCases is not { Count: > 0 })
            return "Inga tidigare projekt tillgängliga.";

        var active = projectCases.Where(c => c.IsActive).ToList();
        if (active.Count == 0)
            return "Inga aktiva projekt tillgängliga.";

        return string.Join("\n", active.Select(c =>
        {
            var name = c.ClientName ?? "Okänt företag";
            var text = c.Text ?? "";
            return string.IsNullOrWhiteSpace(text) ? $"• {name}" : $"• {name}: {text}";
        }));
    }

    protected static string BuildContactSection(ContactPersonContext contact)
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== KONTAKTPERSON ===");
        sb.AppendLine($"Namn: {contact.Name}");
        if (!string.IsNullOrWhiteSpace(contact.Title)) sb.AppendLine($"Titel: {contact.Title}");
        if (!string.IsNullOrWhiteSpace(contact.Email)) sb.AppendLine($"E-post: {contact.Email}");
        if (contact.PersonalHooks?.Any() == true) sb.AppendLine($"Personliga hooks: {string.Join(", ", contact.PersonalHooks)}");
        if (contact.PersonalNews?.Any() == true) sb.AppendLine($"Senaste nyheter: {string.Join(", ", contact.PersonalNews)}");
        if (!string.IsNullOrWhiteSpace(contact.Summary)) sb.AppendLine($"Sammanfattning: {contact.Summary}");
        return sb.ToString().TrimEnd();
    }

    protected static string FormatStepType(SequenceStepType stepType) => stepType switch
    {
        SequenceStepType.Email => "e-postmeddelande",
        SequenceStepType.LinkedInMessage => "LinkedIn-meddelande",
        SequenceStepType.LinkedInConnectionRequest => "LinkedIn-kontaktförfrågan",
        SequenceStepType.LinkedInInteraction => "LinkedIn-interaktion",
        _ => "meddelande"
    };

    private static string SanitizeJson(string json)
    {
        if (json.StartsWith("```json")) json = json[7..];
        else if (json.StartsWith("```")) json = json[3..];
        if (json.EndsWith("```")) json = json[..^3];
        return json.Trim();
    }
}
