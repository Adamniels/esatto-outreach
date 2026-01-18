using System.Text.Json;
using Esatto.Outreach.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace Esatto.Outreach.Infrastructure.Services.Enrichment;

public class CompanyKnowledgeBaseService : ICompanyKnowledgeBaseService
{
    private readonly IGenerativeAIClient _aiClient;
    private readonly ILogger<CompanyKnowledgeBaseService> _logger;

    public CompanyKnowledgeBaseService(IGenerativeAIClient aiClient, ILogger<CompanyKnowledgeBaseService> logger)
    {
        _aiClient = aiClient;
        _logger = logger;
    }

    public async Task<List<KnowledgeSnippet>> AnalyzePagesAsync(List<WebPageContent> pages, CancellationToken ct = default)
    {
        var validPages = pages.Where(p => !string.IsNullOrWhiteSpace(p.BodyText)).ToList();
        if (!validPages.Any()) return new List<KnowledgeSnippet>();

        // Batch pages to save tokens/calls (e.g., 5 pages per batch)
        var batches = validPages.Chunk(5).ToList();
        var allSnippets = new List<KnowledgeSnippet>();

        foreach (var batch in batches)
        {
            try 
            {
                var batchSnippets = await AnalyzeBatchAsync(batch, ct);
                allSnippets.AddRange(batchSnippets);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing batch of {Count} pages", batch.Length);
            }
        }

        return allSnippets;
    }

    private async Task<List<KnowledgeSnippet>> AnalyzeBatchAsync(WebPageContent[] batch, CancellationToken ct)
    {
        // Construct the prompt input
        var pagesInput = batch.Select((p, i) => new 
        {
            Id = i,
            p.Url,
            p.Title,
            p.H1,
            Text = p.BodyText.Length > 2000 ? p.BodyText.Substring(0, 2000) + "..." : p.BodyText // Truncate individual pages to stay within limits
        }).ToList();

        var prompt = $@"
You are an expert content analyst. Your job is to analyze the following {batch.Length} web pages from a company's website.
For EACH page, determine its type and extract specific knowledge.

### PAGE TYPES TO IDENTIFY:
- ""About"": Company overview, values, history, team.
- ""Service"": Specific product or service offering.
- ""Case"": A customer success story or project description.
- ""Methods"": How they work, tech stack, approach.
- ""Other"": News, blog, or irrelevant.

### EXTRACTION RULES:
1. **Summary**: 1-2 sentence summary of the page's core value.
2. **Case Studies**: If the page describes a CLIENT PROJECT, extract structured details (Client Name, Challenge, Solution).
3. **Key Facts**: Extract strict bullet points about the company (e.g., ""Founded in 2012"", ""Works with Retail"", ""Uses Azure"").

### INPUT:
{JsonSerializer.Serialize(pagesInput)}

### OUTPUT FORMAT:
Return a JSON ARRAY of objects, one for each input page, matching this schema:
[
  {{
    ""Url"": ""..."", 
    ""PageTitle"": ""..."",
    ""PageType"": ""About/Service/Case/Methods/Other"",
    ""Summary"": ""..."",
    ""CaseStudies"": [ {{ ""ClientName"": ""..."", ""Challenge"": ""..."", ""Solution"": ""..."" }} ],
    ""KeyFacts"": [ ""..."" ]
  }}
]
";

        var responseText = await _aiClient.GenerateTextAsync(
            userInput: prompt,
            systemPrompt: "You are a precise data extractor. Return JSON only.",
            useWebSearch: false, // Pure analysis of provided text
            temperature: 0.1,
            maxOutputTokens: 4000
        );

        if (string.IsNullOrWhiteSpace(responseText)) 
        {
            _logger.LogWarning("AI returned empty response for page analysis.");
            return new List<KnowledgeSnippet>();
        }

        try
        {
            var cleanJson = responseText;
            // Attempt to find the first '[' and last ']' to handle extra text
            var startIndex = cleanJson.IndexOf('[');
            var endIndex = cleanJson.LastIndexOf(']');
            
            if (startIndex >= 0 && endIndex > startIndex)
            {
                cleanJson = cleanJson.Substring(startIndex, endIndex - startIndex + 1);
            }

            // Also clean markdown if present (redundant if the above works well, but safe)
            if (cleanJson.StartsWith("```"))
            {
                cleanJson = System.Text.RegularExpressions.Regex.Replace(cleanJson, @"^```(\w+)?\s*", "");
                cleanJson = System.Text.RegularExpressions.Regex.Replace(cleanJson, @"\s*```$", "");
            }

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return JsonSerializer.Deserialize<List<KnowledgeSnippet>>(cleanJson, options) ?? new List<KnowledgeSnippet>();
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse AI response for page analysis. Raw: {Raw}", responseText);
            return new List<KnowledgeSnippet>();
        }
    }
}
