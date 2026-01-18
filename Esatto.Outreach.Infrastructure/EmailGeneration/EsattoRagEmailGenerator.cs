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
        if (context.EntityIntelligence == null)
        {
            throw new InvalidOperationException(
                "EsattoRagEmailGenerator requires Entity Intelligence. " +
                "Use EmailGenerationType.UseCollectedData or ensure data collection is completed first.");
        }

        _logger.LogInformation(
            "Generating email using Esatto RAG for prospect {ProspectId}",
            context.EntityIntelligence.ProspectId);

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
        var intelligence = context.EntityIntelligence!;

        // Extract company name from prospect
        var companyName = context.Request.Name ?? "Unknown Company";

        // 1. Add Summarized Context
        if (!string.IsNullOrWhiteSpace(intelligence.SummarizedContext))
        {
            documents.Add(new RagDocument
            {
                DocType = "summary",
                Title = "Company Summary",
                Text = intelligence.SummarizedContext,
                SourceUrl = "AI Analysis",
                Priority = 1.5
            });
        }

        // 2. Add Structured Enrichment Documents
        if (intelligence.EnrichedData != null)
        {
            var ed = intelligence.EnrichedData;

            // Snapshot (what/how/who)
            documents.Add(new RagDocument
            {
                DocType = "snapshot",
                Title = "Company Snapshot",
                Text = $"{ed.Snapshot.WhatTheyDo}. Operates by: {ed.Snapshot.HowTheyOperate}. Targeting: {ed.Snapshot.TargetCustomer}",
                SourceUrl = "AI Enrichment",
                Priority = 1.8
            });

            // Challenges (Confirmed)
            foreach (var c in ed.Challenges.Confirmed)
            {
                documents.Add(new RagDocument
                {
                    DocType = "pain_point",
                    Title = "Confirmed Challenge",
                    Text = $"{c.ChallengeDescription} (Evidence: {c.EvidenceSnippet})",
                    SourceUrl = c.SourceUrl,
                    Priority = 1.9 // Higher priority for confirmed pain
                });
            }

            // Challenges (Inferred)
            foreach (var c in ed.Challenges.Inferred)
            {
                documents.Add(new RagDocument
                {
                    DocType = "pain_point",
                    Title = "Inferred Challenge",
                    Text = $"{c.ChallengeDescription} (Reasoning: {c.Reasoning})",
                    SourceUrl = "AI Inference",
                    Priority = 1.4
                });
            }

            // Outreach Hooks
            foreach (var h in ed.OutreachHooks)
            {
                documents.Add(new RagDocument
                {
                    DocType = "hook",
                    Title = "Outreach Hook",
                    Text = $"{h.HookDescription} (Date: {h.Date}). Why: {h.WhyItMatters}",
                    SourceUrl = h.Source,
                    Priority = 1.6
                });
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
