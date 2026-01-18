using System.Text.Json;
using System.Text.RegularExpressions;
using Esatto.Outreach.Application.Abstractions;
using Esatto.Outreach.Domain.ValueObjects;
using Esatto.Outreach.Infrastructure.Services.Scraping;
using Microsoft.Extensions.Logging;
using Esatto.Outreach.Domain.Entities; // Added for KnowledgeSnippet

namespace Esatto.Outreach.Infrastructure.Services.Enrichment;

public class CompanyEnrichmentService : ICompanyEnrichmentService
{
    private readonly IWebScraperService _webScraper;
    private readonly ICompanyKnowledgeBaseService _knowledgeBase;
    private readonly DuckDuckGoSerpService _serpService;
    private readonly IGenerativeAIClient _aiClient;
    private readonly ILogger<CompanyEnrichmentService> _logger;

    public CompanyEnrichmentService(
        IWebScraperService webScraper,
        ICompanyKnowledgeBaseService knowledgeBase,
        DuckDuckGoSerpService serpService,
        IGenerativeAIClient aiClient,
        ILogger<CompanyEnrichmentService> logger)
    {
        _webScraper = webScraper;
        _knowledgeBase = knowledgeBase;
        _serpService = serpService;
        _aiClient = aiClient;
        _logger = logger;
    }

    public async Task<CompanyEnrichmentResult> EnrichCompanyAsync(string companyName, string domain, CancellationToken ct = default)
    {
        _logger.LogInformation("Starting Robust Company Enrichment for {CompanyName} ({Domain})", companyName, domain);

        // Define the two parallel tracks
        
        // TRACK A: Internal Website Intelligence
        var internalTask = Task.Run(async () => 
        {
            // 1. Scraping (Map)
            var siteData = await _webScraper.ScrapeCompanySiteAsync(domain, ct);
            _logger.LogInformation("Internal Scraping completed. Found {PageCount} relevant pages.", siteData.Pages.Count);

            // 2. Knowledge Extraction (Reduce)
            return await _knowledgeBase.AnalyzePagesAsync(siteData.Pages, ct);
        }, ct);

        // TRACK B: External Intelligence (Deep Search & Crawl) - RUNS IN PARALLEL
        var externalTask = GetExternalIntelligenceAsync(companyName, ct);

        // Wait for both tracks to complete
        await Task.WhenAll(internalTask, externalTask);

        var internalNuggets = await internalTask;
        var externalNuggets = await externalTask;
        
        // Merge Internal + External for the prompt
        // We prioritize Internal for "Truth" but External for "Hooks/Signals"
        // Merge Internal + External for the prompt
        // We prioritize Internal for "Truth" but External for "Hooks/Signals"
        var allNuggets = internalNuggets.Concat(externalNuggets).ToList();

        // --- DEBUG: LOG EVERYTHING ---
        _logger.LogInformation("=== DEBUG: ENRICHMENT DATA DUMP ===");
        _logger.LogInformation("1. INTERNAL NUGGETS ({Count}):\n{Json}", internalNuggets.Count, DumpJson(internalNuggets));
        _logger.LogInformation("2. EXTERNAL NUGGETS ({Count}):\n{Json}", externalNuggets.Count, DumpJson(externalNuggets));
        _logger.LogInformation("3. COMBINED CONTEXT (Input to LLM):\n{Json}", DumpJson(allNuggets));
        // -----------------------------

        // 4. Synthesis
        var finalPrompt = BuildPrompt(companyName, domain, allNuggets);
        
        _logger.LogInformation("=== DEBUG: FINAL LLM PROMPT (Verify Context) ===\n{Prompt}", finalPrompt);

        var responseText = await _aiClient.GenerateTextAsync(
            userInput: finalPrompt,
            systemPrompt: "You are a specialized B2B Analyst. You write comprehensive, detailed reports.",
            useWebSearch: false, // Synthesis uses the provided nuggets
            temperature: 0.1,
            maxOutputTokens: 3000,
            ct: ct
        );

        _logger.LogInformation("=== DEBUG: RAW SYNTHESIS RESPONSE ===\n{Response}", responseText);

        // 5. Parse Result
        var result = ParseResponse(responseText);
        return result;
    }

    private async Task<List<KnowledgeSnippet>> GetExternalIntelligenceAsync(string companyName, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Creating External Intelligence Loop for {CompanyName}", companyName);

            // A. Discovery (AI Search)
            // User Request: "Find 3 latest news items (max 4 months) with date and link for personalized outreach."
            var discoveryPrompt = @$"
RESEARCH TASK: Find the 6 LATEST news/events for: {companyName}
CONSTRAINT: Events must be from the LAST 4 MONTHS (Strict).
SOURCES: Prioritize official Press Releases and LinkedIn Company Page posts. NOTE: Look for dates in the posts.

OUTPUT:
Return a JSON ARRAY of objects:
- ""url"": Direct link to the post or article.
- ""date"": YYYY-MM-DD (Use YYYY-MM-01 if day is unknown).
- ""title"": Headline.
- ""summary"": 5-sentence sales context.

RETURN ONLY JSON: [ {{ ""url"": ""..."", ""date"": ""..."", ... }} ]";
            
            var discoveryResponseText = await _aiClient.GenerateTextAsync(
                userInput: discoveryPrompt,
                systemPrompt: "You are a senior sales researcher. You are PARANOID about correct dates. You prioritize LinkedIn posts.",
                useWebSearch: true,
                temperature: 0.1,
                maxOutputTokens: 1500,
                ct: ct
            );

            // Parse rich result
            _logger.LogInformation("RAW AI NEWS SEARCH RESPONSE:\n{RawResponse}", discoveryResponseText);
            
            var discoveryItems = ParseRichDiscoveryItems(discoveryResponseText);
            _logger.LogInformation("External Discovery found {Count} items.", discoveryItems.Count);

            var finalNuggets = new List<KnowledgeSnippet>();

            // Convert Discovery Items to Nuggets immediately (base layer)
            foreach (var item in discoveryItems)
            {
                finalNuggets.Add(new KnowledgeSnippet
                {
                    SourceUrl = item.Url ?? "",
                    PageTitle = (item.Title ?? "Unknown") + $" ({item.Date ?? "No Date"})",
                    Summary = "AI SEARCH HIT: " + (item.Summary ?? ""),
                    KeyFacts = new List<string> { "External Signal", "Recent News" },
                    PageType = "News",
                    CaseStudies = new List<ExtractedCaseStudy>()
                });
            }

            if (!discoveryItems.Any()) return finalNuggets;

            // B. Deep Crawl (Map) - Attempt to get FULL content to verify/expand
            var tasks = discoveryItems.Select(async item => 
            {
                try 
                {
                     if (string.IsNullOrEmpty(item.Url)) return null;

                     var draggedPage = await _webScraper.ScrapePageAsync(item.Url, ct);
                     if (draggedPage != null && !string.IsNullOrWhiteSpace(draggedPage.BodyText))
                     {
                         // If successful, we can potentially replace/enrich the nugget, 
                         // or just let the Analysis step create a NEW, better nugget.
                         return draggedPage;
                     }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Failed to crawl external URL {Url}: {Message}. Relying on AI Search summary.", item.Url, ex.Message);
                }
                return null;
            });

            var rawPages = await Task.WhenAll(tasks);
            var successfulPages = rawPages.Where(p => p != null).ToList();

            _logger.LogInformation("Deep Crawl successfully retrieved {Count} full pages.", successfulPages.Count);

            // C. Analysis (Reduce)
            if (successfulPages.Any())
            {
                var crawledNuggets = await _knowledgeBase.AnalyzePagesAsync(successfulPages!, ct);
                // We add these. The Synthesis step will see both the "AI Summary" and the "Deep Crawl Detail" 
                // and naturally combine them (or prefer the detailed one).
                finalNuggets.AddRange(crawledNuggets);
            }
            
            return finalNuggets;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute External Intelligence Loop");
            return new List<KnowledgeSnippet>();
        }
    }

    private class DiscoveryItem { public string? Url {get;set;} public string? Date {get;set;} public string? Title {get;set;} public string? Summary {get;set;} }

    private List<DiscoveryItem> ParseRichDiscoveryItems(string jsonContent)
    {
        try
        {
            // Robust cleaning: Find the JSON array [...]
            var startIndex = jsonContent.IndexOf('[');
            var endIndex = jsonContent.LastIndexOf(']');

            if (startIndex == -1 || endIndex == -1 || endIndex <= startIndex)
            {
                // Fallback to simple generic cleanup if no array brackets found (unlikely for this prompt)
                 var cleanJson = Regex.Replace(jsonContent, @"```json\s*", "", RegexOptions.IgnoreCase);
                 cleanJson = Regex.Replace(cleanJson, @"```\s*$", "", RegexOptions.IgnoreCase).Trim();
                 return JsonSerializer.Deserialize<List<DiscoveryItem>>(cleanJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<DiscoveryItem>();
            }

            var jsonPart = jsonContent.Substring(startIndex, endIndex - startIndex + 1);
            return JsonSerializer.Deserialize<List<DiscoveryItem>>(jsonPart, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<DiscoveryItem>();
        }
        catch 
        {
            _logger.LogWarning("Failed to parse JSON from AI Search. Raw: {Raw}", jsonContent);
            return new List<DiscoveryItem>();
        }
    }

    // BuildPrompt follows...

    private string BuildPrompt(string companyName, string domain, List<KnowledgeSnippet> nuggets)
    {
        var aboutNuggets = nuggets.Where(n => n.PageType == "About" || n.PageType == "Service" || n.PageType == "Methods").ToList();
        var caseNuggets = nuggets.Where(n => n.PageType == "Case").ToList();
        
        // External Signals usually come classified as "News", "Other", or just "About" but from external URL.
        // We can group them by SourceUrl domain to distinguish Internal vs External if needed, 
        // OR just trust the PageType logic which works well (a News article on LinkedIn is "News").
        var otherNuggets = nuggets.Where(n => n.PageType == "Other" || n.PageType == "News").ToList();

        var synthesizedAbout = string.Join("\n", aboutNuggets.Select(n => $"- [{n.PageTitle}]({n.SourceUrl}): {n.Summary} Facts: {string.Join(", ", n.KeyFacts)}"));
        var synthesizedCases = string.Join("\n", caseNuggets.SelectMany(n => n.CaseStudies.Select(c => $"- Client: {c.ClientName}. Challenge: {c.Challenge}. Solution: {c.Solution}.")));
        var currentDateUtc = DateTime.UtcNow;
        var cutoffDate = currentDateUtc.AddMonths(-4);
        var recentSignalsList = new List<string>();
        var olderSignalsList = new List<string>();

        foreach (var n in otherNuggets)
        {
            var match = Regex.Match(n.PageTitle, @"\((\d{4}-\d{2}-\d{2})\)");
            if (match.Success && DateTime.TryParse(match.Groups[1].Value, out var date))
            {
                if (date >= cutoffDate)
                {
                    recentSignalsList.Add($"- [{n.PageTitle}]({n.SourceUrl}): {n.Summary}");
                }
                else
                {
                    olderSignalsList.Add($"- [{n.PageTitle}]({n.SourceUrl}): {n.Summary}");
                }
            }
            else
            {
                // No date found, treat as context/recent
                recentSignalsList.Add($"- [{n.PageTitle}]({n.SourceUrl}): {n.Summary}");
            }
        }

        var synthesizedRecent = string.Join("\n", recentSignalsList);
        var synthesizedOlder = string.Join("\n", olderSignalsList);
        
        var currentDateString = currentDateUtc.ToString("yyyy-MM-dd");
        
        return $@"
ANALYZE TARGET: {companyName} ({domain})
CURRENT DATE: {currentDateString}

--- KNOWLEDGE BASE (Internal & External) ---
>> ABOUT & SERVICES:
{synthesizedAbout}

>> PROVEN CASE STUDIES:
{synthesizedCases}

>> EXTERNAL SIGNALS (Recently Published - Use for Hooks):
{synthesizedRecent}

>> ARCHIVED CONTEXT (Older than 4 months - Do NOT use for Hooks, Context Only):
{synthesizedOlder}

=== INSTRUCTIONS ===
Perform a DEEP, COMPREHENSIVE ""Company Enrichment"" analysis.
The user expects a LONG, DETAILED report (Total ~1000 words).
Do NOT be concise. Be verbose and exhaustive.

1. **Company Snapshot (IMPORTANT)**: 
   - Write a FULL NARRATIVE (3-4 paragraphs, ~300 words) for ""WhatTheyDo"". 
   - Describe their history, specific service details, and philosophy depth.
   - ""HowTheyOperate"" should be a detailed paragraph about their delivery model.

2. **Business Challenges**:
   - **Confirmed**: Look for patterns in the CASE STUDIES.
   - **Inferred**: Inferred from their industry/stage.

3. **Outreach Hooks**: 
   - STRICTLY RECENT: Only items from the last 4 months relative to {currentDateString}.
   - If a news item is older than 4 months, DO NOT USE IT as a hook.

=== OUTPUT FORMAT ===
Return ONLY valid JSON matching this structure. NO markdown.
{{
  ""Snapshot"": {{
    ""WhatTheyDo"": ""[REQUIRED: A detailed 250-word narrative describing the company deeply]"",
    ""HowTheyOperate"": ""[REQUIRED: A detailed 150-word paragraph on their operational model]"",
    ""TargetCustomer"": ""[REQUIRED: Detailed 150-word analysis of specific industries and buyer personas]"",
    ""PrimaryValueProposition"": ""[REQUIRED: Detailed 150-word analysis of why customers buy]"",
    ""MarketPosition"": ""[Their standing in the market vs competitors]""
  }},
  ""EvidenceLog"": [
    {{ ""Title"": ""Source Title"", ""Publisher"": ""..."", ""Date"": ""YYYY-MM-DD"", ""Url"": ""..."", ""KeyFactExtracted"": ""..."" }}
  ],
  ""Challenges"": {{
    ""Confirmed"": [
       {{ ""ChallengeDescription"": ""..."", ""EvidenceSnippet"": ""..."", ""SourceUrl"": ""..."" }}
    ],
    ""Inferred"": [
       {{ ""ChallengeDescription"": ""..."", ""Reasoning"": ""..."", ""ConfidenceLevel"": ""High/Medium/Low"" }}
    ]
  }},
  ""Profile"": {{
    ""BusinessModel"": ""..."",
    ""RevenueMotion"": ""..."",
    ""CustomerType"": ""..."",
    ""OperationalComplexity"": ""Low/Medium/High"",
    ""OperationalComplexityReasoning"": ""..."",
    ""DataIntegrationNeeds"": ""Observed/Inferred needs..."",
    ""ScalingStage"": ""Startup/Scale-up/Enterprise"",
    ""ComplianceContext"": ""..."",
    ""TechnologyPosture"": ""..."",
    ""CurrentTechStack"": [""Tech1"", ""Tech2""],
    ""HiringTrends"": [""Trend1"", ""Trend2""],
    ""StrategicPriorities"": [""Priority1"", ""Priority2""],
    ""ProcessMaturity"": ""..."",
    ""NotableConstraints"": ""..."",
    ""FieldConfidence"": {{ ""BusinessModel"": ""High"", ""ScalingStage"": ""Medium"" }}
  }},
  ""OutreachHooks"": [
    {{ ""HookDescription"": ""..."", ""Date"": ""YYYY-MM-DD"", ""Source"": ""..."", ""WhyItMatters"": ""..."", ""ConfidenceLevel"": ""High"" }}
  ],
  ""MethodologyUsed"": [""Structured Scraping"", ""Page Analysis"", ""Map-Reduce Synthesis""],
  ""OpenQuestions"": [""...""]
}}
";
    }

    private CompanyEnrichmentResult ParseResponse(string jsonContent)
    {
        try
        {
            // Robust cleaning: Find the JSON object {...}
            var startIndex = jsonContent.IndexOf('{');
            var endIndex = jsonContent.LastIndexOf('}');

            string cleanJson;
            if (startIndex >= 0 && endIndex > startIndex)
            {
                cleanJson = jsonContent.Substring(startIndex, endIndex - startIndex + 1);
            }
            else
            {
                cleanJson = Regex.Replace(jsonContent, @"```json\s*", "", RegexOptions.IgnoreCase);
                cleanJson = Regex.Replace(cleanJson, @"```\s*$", "", RegexOptions.IgnoreCase);
                cleanJson = cleanJson.Trim();
            }

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var result = JsonSerializer.Deserialize<CompanyEnrichmentResult>(cleanJson, options);
            
            if (result == null) throw new JsonException("Parsed null result");
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse CompanyEnrichmentResult. JSON: {Json}", jsonContent);
            // Return empty/safe result to avoid crashing flow, but log error
            return new CompanyEnrichmentResult
            {
                Snapshot = new CompanySnapshot { WhatTheyDo = "Error parsing", HowTheyOperate = "Error", TargetCustomer = "Error", PrimaryValueProposition = "Error" },
                EvidenceLog = new List<EvidenceSource>(),
                Challenges = new BusinessChallenges { Confirmed = new List<ConfirmedChallenge>(), Inferred = new List<InferredChallenge>() },
                Profile = new SolutionRelevantProfile { BusinessModel = "Unknown", RevenueMotion = "Unknown", CustomerType = "Unknown", OperationalComplexity = "Unknown" },
                OutreachHooks = new List<CompanyOutreachHook>(),
                MethodologyUsed = new List<string> { "Error" },
                OpenQuestions = new List<string> { "Failed to parse JSON" }
            };
        }
    }

    private string DumpJson(object? obj)
    {
        try
        {
            if (obj == null) return "null";
            return JsonSerializer.Serialize(obj, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            return $"Error serializing log: {ex.Message}";
        }
    }
}
