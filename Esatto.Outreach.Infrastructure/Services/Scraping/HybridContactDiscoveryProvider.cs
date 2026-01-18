using System.Text.Json;
using System.Text.RegularExpressions;
using Esatto.Outreach.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace Esatto.Outreach.Infrastructure.Services.Scraping;

public class HybridContactDiscoveryProvider : IContactDiscoveryProvider
{
    private readonly IWebScraperService _webScraper;
    private readonly DuckDuckGoSerpService _serpService;
    private readonly IGenerativeAIClient _aiClient;
    private readonly ILogger<HybridContactDiscoveryProvider> _logger;

    public HybridContactDiscoveryProvider(
        IWebScraperService webScraper,
        DuckDuckGoSerpService serpService,
        IGenerativeAIClient aiClient,
        ILogger<HybridContactDiscoveryProvider> logger)
    {
        _webScraper = webScraper;
        _serpService = serpService;
        _aiClient = aiClient;
        _logger = logger;
    }

    public async Task<List<ProspectCandidate>> FindDecisionMakersAsync(string companyName, string domain, CancellationToken ct = default)
    {
        _logger.LogInformation("Starting Hybrid Contact Discovery for {CompanyName} ({Domain})", companyName, domain);

        // 1. Define Parallel Tasks
        
        // Task A: Web Scraper
        var scrapeTask = Task.Run(async () => 
        {
            try 
            {
                var data = await _webScraper.ScrapeCompanySiteAsync(domain, ct);
                _logger.LogInformation("Method [WebScraper]: Found {CharCount} chars of text", data.BodyText.Length);
                return data.BodyText;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Method [WebScraper]: Failed");
                return "";
            }
        }, ct);

        // Task B: DuckDuckGo SERP
        var serpTask = Task.Run(async () =>
        {
            try
            {
                var queries = new[]
                {
                    $"site:linkedin.com/in/ \"{companyName}\" CEO OR Founder OR Owner OR President",
                    $"site:linkedin.com/in/ \"{companyName}\" CFO OR \"Chief Financial Officer\" OR \"Finance Director\"",
                    $"site:linkedin.com/in/ \"{companyName}\" CMO OR \"Marketing Manager\" OR \"Head of Marketing\"",
                    $"site:linkedin.com/in/ \"{companyName}\" \"Sales Director\" OR \"Head of Sales\" OR \"Business Development\"",
                    $"site:linkedin.com/in/ \"{companyName}\" CTO OR CIO OR \"Head of Tech\" OR \"IT Director\"",
                    $"site:linkedin.com/in/ \"{companyName}\" VP OR Director" // Broad catch-all
                };
                
                var results = await Task.WhenAll(queries.Select(q => _serpService.SearchAsync(q)));
                var flatResults = results.SelectMany(x => x).ToList();
                _logger.LogInformation("Method [DuckDuckGo]: Found {Count} SERP results", flatResults.Count);
                return flatResults;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Method [DuckDuckGo]: Failed");
                return new List<SerpResult>();
            }
        }, ct);

        // Task C: OpenAI Web Search (The "Verification" Method)
        var openAiSearchTask = Task.Run(async () =>
        {
            try
            {
                // Broader prompt to find a list of people
                var searchPrompt = $"Find a list of key decision makers, executives, and management team members for {companyName} ({domain}). " +
                                   $"Include the CEO, CFO, CTO, VP of Sales, VP of Marketing, and other Directors. " +
                                   $"List at least 10 people if possible with their exact titles.";
                                   
                var responseText = await _aiClient.GenerateTextAsync(
                    userInput: searchPrompt,
                    systemPrompt: "You are an expert researcher. Find as many relevant senior contacts as possible.",
                    useWebSearch: true, // Enable Browsing
                    temperature: 0.3, // Slightly higher temp for broader search
                    maxOutputTokens: 1500,
                    ct: ct
                );
                _logger.LogInformation("Method [OpenAI Web Search]: Call completed. Response length: {Length}", responseText.Length);
                return responseText;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Method [OpenAI Web Search]: Failed");
                return "";
            }
        }, ct);


        // 2. Wait for all to complete
        await Task.WhenAll(scrapeTask, serpTask, openAiSearchTask);

        var siteText = await scrapeTask;
        var serpResults = await serpTask;
        var openAiResearch = await openAiSearchTask;

        // 3. Prepare Aggregated Context
        
        // Truncate site text carefully
        var truncatedSiteText = siteText.Length > 15000 ? siteText[..15000] + "..." : siteText;
        
        var serpSummary = string.Join("\n", serpResults.Select(r => $"- {r.Title}: {r.Snippet} ({r.Link})"));

        var finalPrompt = $@"
We are looking for the best contact persons (Decision Makers) at the company '{companyName}' ({domain}) to sell a B2B solution.

--- SOURCE 1: Company Website Scraping ---
""{truncatedSiteText}""

--- SOURCE 2: LinkedIn Search Results (DuckDuckGo) ---
{serpSummary}

--- SOURCE 3: AI Web Search Findings ---
{openAiResearch}


TASK:
1. Synthesize information from ALL sources to find real people working at this company.
2. Deduplicate people found in multiple sources.
3. Determine their Role/Title and LinkedIn URL.
4. Try to find or infer their email (if explicitly visible).
5. Rate them 0-100 on how relevant they are for a sales outreach. (C-Level, VPs, Directors are high priority).
6. Provide a short Rationale explanation citing which source(s) the info came from (e.g., ""Found on Website and confirmed via AI Search"").

OUTPUT FORMAT:
Return a JSON array of objects. **List up to 10 relevant candidates**, sorted by ConfidenceScore (Relevance).
Do not include markdown formatting (```json).
[
  {{
    ""Name"": ""string"",
    ""Title"": ""string"",
    ""LinkedInUrl"": ""string or null"",
    ""Source"": ""string (e.g. Website, LinkedIn, Web Search)"",
    ""ConfidenceScore"": number (0-100),
    ""Email"": ""string or null"",
    ""Rationale"": ""string""
  }}
]
";

        // 4. Final LLM Processing
        _logger.LogInformation("Sending aggregated context to LLM for final processing...");
        
        var finalResponseText = await _aiClient.GenerateTextAsync(
            userInput: finalPrompt,
            systemPrompt: "You are an expert sales researcher. Extract structured contact data from the provided context.",
            useWebSearch: false, // No need to search again, we have the context
            temperature: 0.1, 
            maxOutputTokens: 2000,
            ct: ct
        );

        // 5. Parse JSON
        var json = CleanJson(finalResponseText);
        try
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var candidates = JsonSerializer.Deserialize<List<ProspectCandidate>>(json, options);
            _logger.LogInformation("Hybrid Discovery completed. Found {Count} candidates.", candidates?.Count ?? 0);
            
            if (candidates != null)
            {
                foreach (var c in candidates)
                {
                    _logger.LogInformation("  -> DETECTED CANDIDATE: {Name} ({Title}) | SOURCE: {Source} | RATIONALE: {Rationale} | SCORE: {Score}", 
                        c.Name, c.Title, c.Source, c.Rationale, c.ConfidenceScore);
                }
            }

            return candidates ?? new List<ProspectCandidate>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing contacts JSON. Response was: {Response}", json);
            return new List<ProspectCandidate>();
        }
    }

    private string CleanJson(string text)
    {
        // Remove markdown code blocks if present
        text = Regex.Replace(text, @"```json\s*", "", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"```\s*$", "", RegexOptions.IgnoreCase);
        return text.Trim();
    }
}
