using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Esatto.Outreach.Application.Abstractions;
using Esatto.Outreach.Application.DTOs;
using Esatto.Outreach.Domain.Entities;
using Esatto.Outreach.Infrastructure.Common;

namespace Esatto.Outreach.Infrastructure.EmailGeneration;

public sealed class EsattoRagEmailGenerator : ICustomEmailGenerator
{
    private readonly HttpClient _http;
    private readonly EsattoRagOptions _options;
    private readonly ILogger<EsattoRagEmailGenerator> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public EsattoRagEmailGenerator(
           HttpClient http,
           IOptions<EsattoRagOptions> options,
           ILogger<EsattoRagEmailGenerator> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;

        // Set base adress from config
        _http.BaseAddress = new Uri(_options.BaseUrl);
        _http.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);

        // If I want authentication later
        if (!string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _options.ApiKey);
        }
    }

    public async Task<CustomEmailDraftDto> GenerateAsync(
        EmailGenerationContext context,
        CancellationToken cancellationToken = default)
    {
        // Validate that we have collected soft data
        if (context.SoftData == null)
        {
            throw new InvalidOperationException(
                "EsattoRagEmailGenerator requires collected company data (SoftData). " +
                "Use EmailGenerationType.UseCollectedData or ensure data collection is completed first.");
        }

        _logger.LogInformation(
            "Generating email using Esatto RAG for prospect {ProspectId}",
            context.SoftData.ProspectId);

        // Build RAG request from context
        var ragRequest = BuildRagRequest(context);

        // Send request to RAG API
        var ragResponse = await SendRagRequestAsync(ragRequest, cancellationToken);

        // Log metadata for debugging and analytics
        if (ragResponse.Metadata != null)
        {
            _logger.LogInformation(
                "RAG generation completed in {GenerationTimeMs}ms. " +
                "Retrieved cases: {Cases}. Company chunks used: {ChunksUsed}. " +
                "Embedding model: {EmbeddingModel}. LLM model: {LlmModel}.",
                ragResponse.Metadata.GenerationTimeMs,
                string.Join(", ", ragResponse.Metadata.RetrievedEsattoCases),
                ragResponse.Metadata.CompanyChunksUsed,
                ragResponse.Metadata.EmbeddingModel,
                ragResponse.Metadata.LlmModel);
        }

        // Map RAG response to CustomEmailDraftDto
        return new CustomEmailDraftDto(
            Title: ragResponse.Subject ?? "Samarbetsmöjlighet",
            BodyPlain: ragResponse.Body,
            BodyHTML: ConvertToHtml(ragResponse.Body)
        );
    }

    private static EsattoRagRequest BuildRagRequest(EmailGenerationContext context)
    {
        var documents = new List<RagDocument>();
        var softData = context.SoftData!;

        // Extract company name from prospect
        var companyName = context.Request.Name ?? "Unknown Company";

        // Add personalization hooks as documents (high priority)
        if (!string.IsNullOrWhiteSpace(softData.HooksJson))
        {
            try
            {
                var hooks = JsonSerializer.Deserialize<List<JsonElement>>(softData.HooksJson);
                if (hooks != null)
                {
                    foreach (var hook in hooks)
                    {
                        var text = hook.TryGetProperty("text", out var textProp) ? textProp.GetString() : null;
                        var source = hook.TryGetProperty("source", out var sourceProp) ? sourceProp.GetString() : "Unknown";
                        
                        if (!string.IsNullOrWhiteSpace(text))
                        {
                            documents.Add(new RagDocument
                            {
                                DocType = "other",
                                Title = $"Personalization Hook - {source}",
                                Text = text,
                                SourceUrl = source ?? "N/A",
                                Priority = 1.3
                            });
                        }
                    }
                }
            }
            catch (JsonException)
            {
                // Skip if JSON is invalid
            }
        }

        // Add recent events (high priority - timely)
        if (!string.IsNullOrWhiteSpace(softData.RecentEventsJson))
        {
            try
            {
                var events = JsonSerializer.Deserialize<List<JsonElement>>(softData.RecentEventsJson);
                if (events != null)
                {
                    foreach (var evt in events)
                    {
                        var title = evt.TryGetProperty("title", out var titleProp) ? titleProp.GetString() : null;
                        var date = evt.TryGetProperty("date", out var dateProp) ? dateProp.GetString() : null;
                        var type = evt.TryGetProperty("type", out var typeProp) ? typeProp.GetString() : "event";
                        var url = evt.TryGetProperty("url", out var urlProp) ? urlProp.GetString() : "N/A";
                        
                        if (!string.IsNullOrWhiteSpace(title))
                        {
                            var text = date != null ? $"{title} ({date})" : title;
                            documents.Add(new RagDocument
                            {
                                DocType = "other",
                                Title = $"Event: {title}",
                                Text = text,
                                SourceUrl = url ?? "N/A",
                                Priority = 1.5
                            });
                        }
                    }
                }
            }
            catch (JsonException)
            {
                // Skip if JSON is invalid
            }
        }

        // Add news items (high priority - timely and relevant)
        if (!string.IsNullOrWhiteSpace(softData.NewsItemsJson))
        {
            try
            {
                var news = JsonSerializer.Deserialize<List<JsonElement>>(softData.NewsItemsJson);
                if (news != null)
                {
                    foreach (var item in news)
                    {
                        var headline = item.TryGetProperty("headline", out var headlineProp) ? headlineProp.GetString() : null;
                        var date = item.TryGetProperty("date", out var dateProp) ? dateProp.GetString() : null;
                        var source = item.TryGetProperty("source", out var sourceProp) ? sourceProp.GetString() : "Unknown";
                        var url = item.TryGetProperty("url", out var urlProp) ? urlProp.GetString() : "N/A";
                        
                        if (!string.IsNullOrWhiteSpace(headline))
                        {
                            var text = date != null ? $"{headline} ({date}, {source})" : $"{headline} ({source})";
                            documents.Add(new RagDocument
                            {
                                DocType = "press_release",
                                Title = headline,
                                Text = text,
                                SourceUrl = url ?? "N/A",
                                Priority = 1.5
                            });
                        }
                    }
                }
            }
            catch (JsonException)
            {
                // Skip if JSON is invalid
            }
        }

        // Add social activity (standard priority)
        if (!string.IsNullOrWhiteSpace(softData.SocialActivityJson))
        {
            try
            {
                var social = JsonSerializer.Deserialize<List<JsonElement>>(softData.SocialActivityJson);
                if (social != null)
                {
                    foreach (var post in social)
                    {
                        var platform = post.TryGetProperty("platform", out var platformProp) ? platformProp.GetString() : "Social";
                        var text = post.TryGetProperty("text", out var textProp) ? textProp.GetString() : null;
                        var url = post.TryGetProperty("url", out var urlProp) ? urlProp.GetString() : "N/A";
                        
                        if (!string.IsNullOrWhiteSpace(text))
                        {
                            documents.Add(new RagDocument
                            {
                                DocType = "linkedin",
                                Title = $"{platform} Post",
                                Text = text,
                                SourceUrl = url ?? "N/A",
                                Priority = 1.0
                            });
                        }
                    }
                }
            }
            catch (JsonException)
            {
                // Skip if JSON is invalid
            }
        }

        // Extract contact person name from request
        var contactPerson = context.Request.Name ?? "there";

        // Build preferences
        var preferences = new RagPreferences
        {
            Tone = "professional",
            Language = "sv",
            MaxLength = 400,
            FocusAreas = new List<string>() // Could be extracted from instructions in the future
        };

        return new EsattoRagRequest
        {
            CompanyName = companyName,
            Documents = documents,
            Preferences = preferences,
            ContactPerson = contactPerson
        };
    }

    private async Task<EsattoRagResponse> SendRagRequestAsync(
        EsattoRagRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug(
                "Sending RAG request for company {CompanyName} with {DocumentCount} documents",
                request.CompanyName,
                request.Documents.Count);

            var response = await _http.PostAsJsonAsync(
                "/api/v1/draft-email",
                request,
                JsonOptions,
                cancellationToken);

            response.EnsureSuccessStatusCode();

            var ragResponse = await response.Content.ReadFromJsonAsync<EsattoRagResponse>(
                JsonOptions,
                cancellationToken);

            if (ragResponse == null)
            {
                throw new InvalidOperationException("RAG API returned null response");
            }

            return ragResponse;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to connect to Esatto RAG server at {BaseUrl}", _options.BaseUrl);
            throw new InvalidOperationException(
                $"Failed to generate email using Esatto RAG server at {_options.BaseUrl}. " +
                $"Ensure the RAG server is running. Error: {ex.Message}", ex);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse RAG API response");
            throw new InvalidOperationException(
                "Failed to parse response from Esatto RAG server. " +
                $"The API response format may have changed. Error: {ex.Message}", ex);
        }
    }

    private static string ConvertToHtml(string plainText)
    {
        if (string.IsNullOrWhiteSpace(plainText))
        {
            return string.Empty;
        }

        // Simple conversion: replace newlines with <br> and wrap in paragraph
        var html = plainText
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\n", "<br>");

        return $"<p>{html}</p>";
    }
}
