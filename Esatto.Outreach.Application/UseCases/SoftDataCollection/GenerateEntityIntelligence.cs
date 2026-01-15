using Esatto.Outreach.Application.Abstractions;
using Esatto.Outreach.Application.DTOs;
using Esatto.Outreach.Domain.Entities;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Esatto.Outreach.Application.UseCases.SoftDataCollection;

public sealed class GenerateEntityIntelligence
{
    private readonly IEntityIntelligenceRepository _enrichmentRepo;
    private readonly IProspectRepository _prospectRepo;
    private readonly IWebScraperService _scraper;
    private readonly IContactDiscoveryProvider _contactDiscovery;
    private readonly IOpenAIChatClient _aiClient;
    private readonly ILogger<GenerateEntityIntelligence> _logger;

    public GenerateEntityIntelligence(
        IEntityIntelligenceRepository enrichmentRepo,
        IProspectRepository prospectRepo,
        IWebScraperService scraper,
        IContactDiscoveryProvider contactDiscovery,
        IOpenAIChatClient aiClient,
        ILogger<GenerateEntityIntelligence> logger)
    {
        _enrichmentRepo = enrichmentRepo;
        _prospectRepo = prospectRepo;
        _scraper = scraper;
        _contactDiscovery = contactDiscovery;
        _aiClient = aiClient;
        _logger = logger;
    }

    public async Task<EntityIntelligenceDto> Handle(Guid prospectId, CancellationToken ct = default)
    {
        // 1. Fetch Prospect (ReadOnly for scraping context)
        var prospect = await _prospectRepo.GetByIdReadOnlyAsync(prospectId, ct)
            ?? throw new KeyNotFoundException($"Prospect {prospectId} not found.");

        _logger.LogInformation("Starting Entity Intelligence Enrichment for {Company}", prospect.Name);

        // 2. Layer 1: Site Scraping & AI Analysis
        var siteData = await _scraper.ScrapeCompanySiteAsync(prospect.GetPrimaryWebsite() ?? prospect.Name + ".com", ct);

        // AI Analysis of Company
        var companyAnalysisPrompt = $@"
Analyze this B2B company based on their website text.
You are an expert agency analyst finding sales intelligence for a digital agency (web/app dev).

Extract the following structured data:
1. SUMMARY: A concise executive summary of what they do.
2. KEY_VALUE_PROPS: List of 3-5 key value propositions they offer.
3. TECH_STACK: Any technologies mentioned (e.g. .NET, React, AWS) or methodologies.
4. CASE_STUDIES: Identify specific client cases or projects. Format: Client Name, The Challenge, The Solution, The Outcome.
5. NEWS: Recent news or events.
6. HIRING: Any open roles mentioned.

Format as valid JSON matching this structure:
{{
  ""summary"": ""string"",
  ""keyValueProps"": [""string""],
  ""techStack"": [""string""],
  ""caseStudies"": [
    {{ ""client"": ""string"", ""challenge"": ""string"", ""solution"": ""string"", ""outcome"": ""string"" }}
  ],
  ""news"": [
     {{ ""date"": ""string"", ""description"": ""string"", ""source"": ""context"" }}
  ],
  ""hiring"": [
     {{ ""role"": ""string"", ""date"": ""string"", ""source"": ""context"" }}
  ]
}}

WEBSITE TITLE: {siteData.Title}
DESCRIPTION: {siteData.MetaDescription}
TEXT: {siteData.BodyText}";

        // Real AI Analysis call
        var (aiResponse, _) = await _aiClient.SendChatMessageAsync(
            userInput: companyAnalysisPrompt,
            systemPrompt: "You are an expert B2B analyst helping a digital agency find sales hooks.",
            previousResponseId: null,
            useWebSearch: false,
            temperature: 0.3,
            maxOutputTokens: 2000,
            initialMailContext: null,
            ct: ct
        );

        EnrichedCompanyDataDto enrichedData = new(
            Summary: "",
            KeyValueProps: new(),
            TechStack: new(),
            CaseStudies: new(),
            News: new(),
            Hiring: new()
        );
        string summary = "";

        try
        {
            var jsonText = aiResponse?.AiMessage; // Null check
            if (!string.IsNullOrWhiteSpace(jsonText))
            {
                // Clean markdown if present
                if (jsonText.Contains("```json"))
                {
                    jsonText = jsonText.Split("```json")[1].Split("```")[0].Trim();
                }
                else if (jsonText.Contains("```"))
                {
                    jsonText = jsonText.Split("```")[1].Split("```")[0].Trim();
                }

                // Parse into Rich DTO
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var parsedData = JsonSerializer.Deserialize<EnrichedCompanyDataDto>(jsonText, options);

                if (parsedData != null)
                {
                    enrichedData = parsedData;
                    summary = parsedData.Summary;
                }
            }
            else
            {
                _logger.LogWarning("AI response was null or empty.");
                summary = "AI analysis processing failed (empty response).";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse AI analysis response. Using raw text as summary.");
            summary = aiResponse?.AiMessage ?? "Analysis failed.";
        }

        // Serialize the RICH object into the CompanyHooksJson column (Upgrade)
        var companyHooksJson = JsonSerializer.Serialize(enrichedData);


        // 2b. Layer 1.5: Web Search Research (Entity Intelligence via Search)
        // User requested: "AI call to open AI with websearch"
        // 2b. Layer 1.5: Web Search Research (Entity Intelligence via Search)
        // User requested: "AI call to open AI with websearch"
        var webSearchPrompt = $@"
Research the company '{prospect.Name}' (Website: {prospect.GetPrimaryWebsite()}).
Find 3 RECENT news items, press releases, or external events (podcasts, webinars) from the last 6 months.
Focus on things showing growth, change, or new initiatives.

Format as valid JSON matching:
{{
  ""news"": [
     {{ ""date"": ""string (approx)"", ""description"": ""string"", ""source"": ""context domain/url"" }}
  ]
}}";

        var (searchResponse, _) = await _aiClient.SendChatMessageAsync(
             userInput: webSearchPrompt,
             systemPrompt: "You are a research assistant finding recent B2B news.",
             previousResponseId: null,
             useWebSearch: true, // ENABLE WEB SEARCH
             temperature: 0.3,
             maxOutputTokens: 1000,
             initialMailContext: null,
             ct: ct
         );

        try
        {
            var jsonText = searchResponse?.AiMessage;
            if (!string.IsNullOrWhiteSpace(jsonText))
            {
                if (jsonText.Contains("```json")) jsonText = jsonText.Split("```json")[1].Split("```")[0].Trim();
                else if (jsonText.Contains("```")) jsonText = jsonText.Split("```")[1].Split("```")[0].Trim();

                // Parse structured news
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                // Quick DTO for search response
                var searchResult = JsonSerializer.Deserialize<EnrichedCompanyDataDto>(jsonText, options);

                if (searchResult?.News != null)
                {
                    if (enrichedData.News == null) enrichedData = enrichedData with { News = new() };
                    enrichedData.News.AddRange(searchResult.News);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse Web Search response.");
            if (searchResponse?.AiMessage != null)
            {
                // Fallback: Add raw text as a 'News' item if parsing fails
                if (enrichedData.News == null) enrichedData = enrichedData with { News = new() };

                var rawMsg = searchResponse.AiMessage;
                var safeSnippet = rawMsg.Length > 200 ? rawMsg[..200] + "..." : rawMsg;
                enrichedData.News.Add(new NewsEventDto("Unknown", $"Raw Search Result: {safeSnippet}", "Web Search"));
            }
        }

        // Re-serialize with added news
        companyHooksJson = JsonSerializer.Serialize(enrichedData);

        // 3. Contact Discovery (Use ReadOnly prospect data)
        // Perform this BEFORE loading the tracked entity to avoid stale data during long API calls.
        var contacts = await _contactDiscovery.FindDecisionMakersAsync(prospect.Name, prospect.GetPrimaryWebsite() ?? "", ct);

        // 4. LOAD TRACKED ENTITIES & TRANSACTION START
        // Now that all external/slow operations are done, load the fresh entities for update.
        var trackedProspect = await _prospectRepo.GetByIdAsync(prospectId, ct)
             ?? throw new KeyNotFoundException($"Prospect {prospectId} not found.");

        var existingIntelligence = await _enrichmentRepo.GetByProspectIdAsync(prospectId, ct);

        // 5. Apply Contact Updates
        if (contacts.Any())
        {
            foreach (var c in contacts)
            {
                _logger.LogInformation("Found detected contact: {Name} ({Title})", c.Name, c.Title);

                try
                {
                    // Check if contact already exists
                    var existingContact = trackedProspect.ContactPersons
                        .FirstOrDefault(cp => cp.Name.Equals(c.Name, StringComparison.OrdinalIgnoreCase));

                    if (existingContact == null)
                    {
                        var newContact = ContactPerson.Create(prospectId, c.Name, c.Title, null, c.LinkedInUrl);
                        await _prospectRepo.AddContactPersonAsync(newContact, ct);
                        _logger.LogInformation("Added new contact person: {Name}", c.Name);
                    }
                    else
                    {
                        _logger.LogDebug("Contact person {Name} already exists, skipping", c.Name);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to add contact {Name}, skipping", c.Name);
                }
            }
        }

        // 6. Apply Entity Intelligence Updates
        if (existingIntelligence is null)
        {
            existingIntelligence = EntityIntelligence.Create(
                prospectId,
                companyHooksJson,
                "[]",
                summary,
                JsonSerializer.Serialize(new { url = siteData.Url, type = "website_and_search" })
            );
            // Ensure it's added to context
            await _enrichmentRepo.AddAsync(existingIntelligence, ct);
        }
        else
        {
            existingIntelligence.UpdateResearch(
                companyHooksJson: companyHooksJson,
                summarizedContext: summary
            );
            // No need to call UpdateAsync explicitly if we save trackedProspect below, 
            // provided the context is shared. But for safety/clarity we rely on EF tracking.
        }

        // 7. Link & Save (Single Transaction)
        // Linking by ID ensures the relationship is set
        if (trackedProspect.EntityIntelligenceId != existingIntelligence.Id)
        {
            trackedProspect.LinkEntityIntelligence(existingIntelligence.Id);
        }

        // Save the aggregate root (Prospect). This commits all changes to Prospect, Contacts, and Intelligence.
        await _prospectRepo.UpdateAsync(trackedProspect, ct);

        return EntityIntelligenceDto.FromEntity(existingIntelligence);
    }
}
