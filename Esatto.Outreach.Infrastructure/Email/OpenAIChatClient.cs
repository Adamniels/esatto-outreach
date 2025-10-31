using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Esatto.Outreach.Application.Abstractions;
using Esatto.Outreach.Application.DTOs;
using Microsoft.Extensions.Options;

namespace Esatto.Outreach.Infrastructure.Email;

public sealed class OpenAiChatClient : IOpenAIChatClient
{
    private readonly HttpClient _http;
    private readonly OpenAiOptions _options;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public OpenAiChatClient(HttpClient http, IOptions<OpenAiOptions> options)
    {
        _http = http;
        _http.BaseAddress = new Uri("https://api.openai.com/");
        _options = options.Value;
    }

    public async Task<(ChatResponseDto response, string ResponseId)> SendChatMessageAsync(
        string userInput,
        string? systemPrompt,
        string? previousResponseId,
        bool? useWebSearch,
        double? temperature,
        int? maxOutputTokens,
        string? initialMailContext,
        CancellationToken ct = default)
    {

        var effectiveUseWeb = useWebSearch ?? _options.UseWebSearch;
        var effectiveTemp = temperature ?? _options.DefaultTemperature;
        var effectiveMax = maxOutputTokens ?? _options.DefaultMaxOutputTokens;
        var payload = BuildPayload(
            model: _options.Model,
            userText: userInput,
            systemPrompt: string.IsNullOrWhiteSpace(previousResponseId) ? systemPrompt : null,
            initialMailContext: initialMailContext,
            previousResponseId: previousResponseId,
            useWebSearch: effectiveUseWeb,
            temperature: effectiveTemp,
            maxOutputTokens: effectiveMax
        );

        var (outputText, responseId) = await CallResponses(_options.ApiKey, payload, ct);

        // Parse och validera JSON-svaret
        var response = ParseAndValidateResponse(outputText);

        return (response, responseId);
    }

    private static ChatResponseDto ParseAndValidateResponse(string outputText)
    {
        if (string.IsNullOrWhiteSpace(outputText))
        {
            return new ChatResponseDto(
                AiMessage: "[Tomt svar från AI]",
                ImprovedMail: false,
                MailTitle: null,
                MailBodyPlain: null,
                MailBodyHTML: null
            );
        }

        // Trimma och ta bort eventuella code fences
        var cleanText = outputText.Trim();
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

        try
        {
            var dto = JsonSerializer.Deserialize<ChatResponseDto>(cleanText, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            });

            if (dto == null)
            {
                throw new JsonException("Deserialisering returnerade null");
            }

            // Validera: Om ImprovedMail är true måste mejlfälten finnas
            if (dto.ImprovedMail)
            {
                if (string.IsNullOrWhiteSpace(dto.MailTitle) ||
                    string.IsNullOrWhiteSpace(dto.MailBodyPlain) ||
                    string.IsNullOrWhiteSpace(dto.MailBodyHTML))
                {
                    // AI påstod att det finns mejl men fälten saknas - sätt ImprovedMail till false
                    return dto with
                    {
                        ImprovedMail = false,
                        MailTitle = null,
                        MailBodyPlain = null,
                        MailBodyHTML = null
                    };
                }
            }

            return dto;
        }
        catch (JsonException)
        {
            // Om JSON parsing misslyckas, returnera texten som ett vanligt meddelande
            return new ChatResponseDto(
                AiMessage: $"[Kunde inte parsa JSON-svar. Rå text:]\n{cleanText}",
                ImprovedMail: false,
                MailTitle: null,
                MailBodyPlain: null,
                MailBodyHTML: null
            );
        }
    }

    private static object BuildPayload(
        string model,
        string userText,
        string? systemPrompt,
        string? initialMailContext,
        string? previousResponseId,
        bool useWebSearch,
        double temperature,
        int maxOutputTokens)
    {
        var input = new List<object>();

        if (!string.IsNullOrWhiteSpace(systemPrompt))
        {
            input.Add(new
            {
                role = "system",
                content = new object[]
                {
                      new { type = "input_text", text = systemPrompt }
                }
            });
        }

        input.Add(new
        {
            role = "user",
            content = new object[]
            {
                  new { type = "input_text", text = userText }
            }
        });


        if (!string.IsNullOrWhiteSpace(initialMailContext))
        {
            input.Add(new
            {
                role = "user",
                content = new object[]
                {
                    new { type = "input_text", text = initialMailContext }
                }
            });
        }

        var dict = new Dictionary<string, object?>()
        {
            ["model"] = model,
            ["input"] = input,
            ["temperature"] = temperature,
            ["max_output_tokens"] = maxOutputTokens,
            ["tool_choice"] = "auto",
            ["tools"] = useWebSearch ? new object[] { new { type = "web_search" } } : Array.Empty<object>()
        };

        if (!string.IsNullOrWhiteSpace(previousResponseId))
        {
            dict["previous_response_id"] = previousResponseId;
        }

        return dict;
    }

    private async Task<(string outputText, string responseId)> CallResponses(
        string apiKey, object payload, CancellationToken ct)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, "v1/responses");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var reqBody = JsonSerializer.Serialize(payload, JsonOpts);
        req.Content = new StringContent(reqBody, Encoding.UTF8, "application/json");

        using var resp = await _http.SendAsync(req, ct);
        var body = await resp.Content.ReadAsStringAsync(ct);

        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException($"OpenAI HTTP {(int)resp.StatusCode}: {body}");

        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        var text = ExtractOutputText(root);
        var respId = ExtractResponseId(root);

        if (string.IsNullOrWhiteSpace(respId))
            throw new InvalidOperationException("Kunde inte läsa ut response_id från svaret.");

        return (text, respId);
    }

    private static string ExtractOutputText(JsonElement root)
    {
        var sb = new StringBuilder();

        if (root.TryGetProperty("output", out var outputArr) && outputArr.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in outputArr.EnumerateArray())
            {
                if (item.TryGetProperty("content", out var contentArr) && contentArr.ValueKind == JsonValueKind.Array)
                {
                    foreach (var c in contentArr.EnumerateArray())
                    {
                        if (c.TryGetProperty("type", out var t) &&
                            t.GetString() == "output_text" &&
                            c.TryGetProperty("text", out var txt))
                        {
                            var s = txt.GetString();
                            if (!string.IsNullOrEmpty(s)) sb.AppendLine(s);
                        }
                    }
                }
            }
        }

        return sb.ToString();
    }

    private static string ExtractResponseId(JsonElement root)
    {
        if (root.TryGetProperty("id", out var id))
            return id.GetString() ?? "";
        return "";
    }

}
