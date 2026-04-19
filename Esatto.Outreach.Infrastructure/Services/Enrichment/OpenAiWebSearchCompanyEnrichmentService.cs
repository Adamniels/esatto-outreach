using System.Text.Json;
using System.Text.RegularExpressions;
using Esatto.Outreach.Application.Abstractions.Clients;
using Esatto.Outreach.Application.Abstractions.Services;
using Esatto.Outreach.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Esatto.Outreach.Infrastructure.Services.Enrichment;

/// <summary>
/// Single-pass company enrichment via OpenAI Responses API with web_search tool.
/// Replace with an HTTP-backed agent implementation later by swapping <see cref="ICompanyEnrichmentService"/> registration.
/// </summary>
public sealed class OpenAiWebSearchCompanyEnrichmentService : ICompanyEnrichmentService
{
    private readonly IGenerativeAIClient _aiClient;
    private readonly ILogger<OpenAiWebSearchCompanyEnrichmentService> _logger;

    public OpenAiWebSearchCompanyEnrichmentService(
        IGenerativeAIClient aiClient,
        ILogger<OpenAiWebSearchCompanyEnrichmentService> logger)
    {
        _aiClient = aiClient;
        _logger = logger;
    }

    public async Task<CompanyEnrichmentResult> EnrichCompanyAsync(string companyName, string domain, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Company enrichment (single-pass web search) for {CompanyName} ({Domain})",
            companyName,
            domain);

        var currentDate = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var userPrompt = BuildUserPrompt(companyName, domain, currentDate);

        var responseText = await _aiClient.GenerateTextAsync(
            userInput: userPrompt,
            systemPrompt:
                "You are a senior B2B sales intelligence analyst. Use web search to verify facts. " +
                "Ground claims in reputable public sources. " +
                "Output must be a single JSON object only — no markdown, no code fences, no commentary before or after.",
            useWebSearch: true,
            temperature: 0.15,
            maxOutputTokens: 8000,
            ct: ct);

        if (string.IsNullOrWhiteSpace(responseText))
        {
            _logger.LogWarning("Company enrichment returned empty model output for {Company}", companyName);
            return CreateParseErrorResult("Empty model response");
        }

        var result = ParseToResult(responseText);
        TagMethodology(result);
        return result;
    }

    private static string BuildUserPrompt(string companyName, string domain, string currentDateUtc)
    {
        const string jsonSchema = """
{
  "snapshot": {
    "whatTheyDo": "long narrative",
    "howTheyOperate": "paragraph",
    "targetCustomer": "paragraph",
    "primaryValueProposition": "paragraph"
  },
  "evidenceLog": [
    { "title": "", "publisher": "", "date": "YYYY-MM-DD or null", "url": "", "keyFactExtracted": "" }
  ],
  "challenges": {
    "confirmed": [ { "challengeDescription": "", "evidenceSnippet": "", "sourceUrl": "" } ],
    "inferred": [ { "challengeDescription": "", "reasoning": "", "confidenceLevel": "High|Medium|Low" } ]
  },
  "profile": {
    "businessModel": "",
    "revenueMotion": "",
    "customerType": "",
    "operationalComplexity": "Low|Medium|High",
    "operationalComplexityReasoning": "",
    "dataIntegrationNeeds": "",
    "scalingStage": "",
    "complianceContext": "",
    "technologyPosture": "",
    "currentTechStack": [ "" ],
    "hiringTrends": [ "" ],
    "strategicPriorities": [ "" ],
    "processMaturity": "",
    "notableConstraints": "",
    "fieldConfidence": { }
  },
  "outreachHooks": [
    { "hookDescription": "", "date": "YYYY-MM-DD", "source": "URL or publication name", "whyItMatters": "", "confidenceLevel": "" }
  ],
  "methodologyUsed": [ "openai_web_search_single_pass" ],
  "openQuestions": [ "" ]
}
""";

        return $"""
Research the company below using web search. Prefer official site, investor/press pages, regulatory filings, and reputable news.

Company name: {companyName}
Website / domain hint: {domain}
Today's date (UTC): {currentDateUtc}

Produce a deep B2B enrichment. Outreach hooks must prioritize verifiable events from roughly the LAST 4 MONTHS relative to {currentDateUtc}; cite sources (URLs) in EvidenceLog and hook Source fields where possible.

Return ONLY valid JSON matching this exact structure (property names as shown, use camelCase or PascalCase consistently — the parser is case-insensitive):

{jsonSchema}

Do not wrap the JSON in markdown code blocks. Do not add explanatory text outside the JSON.
""";
    }

    private CompanyEnrichmentResult ParseToResult(string raw)
    {
        try
        {
            var json = ExtractJsonObject(raw);
            if (string.IsNullOrEmpty(json))
            {
                _logger.LogWarning("Could not locate a JSON object in model output. Prefix: {Prefix}", Truncate(raw, 800));
                return CreateParseErrorResult("No JSON object found in model output");
            }

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var result = JsonSerializer.Deserialize<CompanyEnrichmentResult>(json, options);
            if (result == null)
                throw new JsonException("Deserialized null");

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse CompanyEnrichmentResult. Raw prefix: {Prefix}", Truncate(raw, 2000));
            return CreateParseErrorResult($"Parse error: {ex.Message}");
        }
    }

    private static void TagMethodology(CompanyEnrichmentResult result)
    {
        const string tag = "openai_web_search_single_pass";
        if (result.MethodologyUsed == null || result.MethodologyUsed.Count == 0)
        {
            result.MethodologyUsed = new List<string> { tag };
            return;
        }

        if (!result.MethodologyUsed.Any(m => string.Equals(m, tag, StringComparison.OrdinalIgnoreCase)))
            result.MethodologyUsed.Insert(0, tag);
    }

    private static string? ExtractJsonObject(string text)
    {
        text = StripMarkdownFences(text).Trim();
        var extracted = TryExtractBalancedJson(text);
        if (!string.IsNullOrEmpty(extracted))
            return extracted;

        var start = text.IndexOf('{');
        var end = text.LastIndexOf('}');
        if (start >= 0 && end > start)
            return text.Substring(start, end - start + 1);

        return null;
    }

    private static string StripMarkdownFences(string text)
    {
        text = Regex.Replace(text, @"^\s*```(?:json)?\s*", "", RegexOptions.IgnoreCase | RegexOptions.Multiline);
        text = Regex.Replace(text, @"\s*```\s*$", "", RegexOptions.Multiline);
        return text.Trim();
    }

    /// <summary>
    /// Extracts outermost JSON object by brace depth, respecting strings.
    /// </summary>
    private static string? TryExtractBalancedJson(string text)
    {
        var start = text.IndexOf('{');
        if (start < 0)
            return null;

        var depth = 0;
        var inString = false;
        var escape = false;

        for (var i = start; i < text.Length; i++)
        {
            var c = text[i];

            if (escape)
            {
                escape = false;
                continue;
            }

            if (c == '\\' && inString)
            {
                escape = true;
                continue;
            }

            if (c == '"')
            {
                inString = !inString;
                continue;
            }

            if (inString)
                continue;

            if (c == '{')
                depth++;
            else if (c == '}')
            {
                depth--;
                if (depth == 0)
                    return text.Substring(start, i - start + 1);
            }
        }

        return null;
    }

    private static CompanyEnrichmentResult CreateParseErrorResult(string message)
    {
        return new CompanyEnrichmentResult
        {
            Snapshot = new CompanySnapshot
            {
                WhatTheyDo = message,
                HowTheyOperate = "",
                TargetCustomer = "",
                PrimaryValueProposition = ""
            },
            EvidenceLog = new List<EvidenceSource>(),
            Challenges = new BusinessChallenges
            {
                Confirmed = new List<ConfirmedChallenge>(),
                Inferred = new List<InferredChallenge>()
            },
            Profile = new SolutionRelevantProfile
            {
                BusinessModel = "Unknown",
                RevenueMotion = "Unknown",
                CustomerType = "Unknown",
                OperationalComplexity = "Unknown"
            },
            OutreachHooks = new List<CompanyOutreachHook>(),
            MethodologyUsed = new List<string> { "openai_web_search_single_pass", "parse_error" },
            OpenQuestions = new List<string> { message }
        };
    }

    private static string Truncate(string s, int max)
    {
        if (string.IsNullOrEmpty(s) || s.Length <= max)
            return s;
        return s[..max] + "...";
    }
}
