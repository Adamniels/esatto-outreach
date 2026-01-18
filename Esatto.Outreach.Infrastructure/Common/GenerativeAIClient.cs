using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Esatto.Outreach.Application.Abstractions;
using Esatto.Outreach.Infrastructure.Common;
using Microsoft.Extensions.Options;

namespace Esatto.Outreach.Infrastructure.Common;

public sealed class GenerativeAIClient : IGenerativeAIClient
{
    private readonly HttpClient _http;
    private readonly OpenAiOptions _options;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public GenerativeAIClient(HttpClient http, IOptions<OpenAiOptions> options)
    {
        _http = http;
        _http.BaseAddress = new Uri("https://api.openai.com/");
        _options = options.Value;
    }

    public async Task<string> GenerateTextAsync(
        string userInput,
        string? systemPrompt = null,
        bool useWebSearch = false,
        double temperature = 0.3,
        int maxOutputTokens = 1500,
        CancellationToken ct = default)
    {
        var payload = BuildPayload(
            model: _options.Model,
            userText: userInput,
            systemPrompt: systemPrompt,
            useWebSearch: useWebSearch,
            temperature: temperature,
            maxOutputTokens: maxOutputTokens
        );

        var outputText = await CallResponses(_options.ApiKey, payload, ct);
        return outputText;
    }

    private static object BuildPayload(
        string model,
        string userText,
        string? systemPrompt,
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

        var dict = new Dictionary<string, object?>()
        {
            ["model"] = model,
            ["input"] = input,
            ["temperature"] = temperature,
            ["max_output_tokens"] = maxOutputTokens,
            ["tool_choice"] = "auto",
            ["tools"] = useWebSearch ? new object[] { new { type = "web_search" } } : Array.Empty<object>()
        };

        return dict;
    }

    private async Task<string> CallResponses(string apiKey, object payload, CancellationToken ct)
    {
        // Using "v1/responses" similar to the Chat Service (assuming internal OpenAI-compatible proxy or specific endpoint wrapper)
        var maxRetries = 5;
        var initialDelay = TimeSpan.FromSeconds(2);

        for (int i = 0; i <= maxRetries; i++)
        {
            using var req = new HttpRequestMessage(HttpMethod.Post, "v1/responses");
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var reqBody = JsonSerializer.Serialize(payload, JsonOpts);
            req.Content = new StringContent(reqBody, Encoding.UTF8, "application/json");

            using var resp = await _http.SendAsync(req, ct);
            
            if (resp.IsSuccessStatusCode)
            {
                var body = await resp.Content.ReadAsStringAsync(ct);
                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;
                return ExtractOutputText(root);
            }

            var errorBody = await resp.Content.ReadAsStringAsync(ct);

            // If it's a 429 (Too Many Requests), wait and retry
            if ((int)resp.StatusCode == 429)
            {
                if (i == maxRetries)
                {
                    throw new InvalidOperationException($"GenerativeAI HTTP 429 (Max Retries Reached): {errorBody}");
                }

                // Parse "Please try again in 21.608s" if possible, otherwise exponential backoff
                var delay = initialDelay * Math.Pow(2, i);
                
                // Simple jitter
                delay = delay.Add(TimeSpan.FromMilliseconds(new Random().Next(0, 500)));

                await Task.Delay(delay, ct);
                continue;
            }

            throw new InvalidOperationException($"GenerativeAI HTTP {(int)resp.StatusCode}: {errorBody}");
        }

        throw new InvalidOperationException("GenerativeAI: Unexpected flow.");
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

        return sb.ToString().Trim();
    }
}
