using Esatto.Outreach.Application.Abstractions;
using Esatto.Outreach.Application.DTOs;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Esatto.Outreach.Application.UseCases.SoftDataCollection;

/// <summary>
/// Enriches a single ContactPerson with personal-level intelligence:
/// - Personal hooks/talking points
/// - Personal news/achievements
/// - Background summary
/// </summary>
public sealed class EnrichContactPerson
{
    private readonly IProspectRepository _prospectRepo;
    private readonly IGenerativeAIClient _aiClient;
    private readonly ILogger<EnrichContactPerson> _logger;

    public EnrichContactPerson(
        IProspectRepository prospectRepo,
        IGenerativeAIClient aiClient,
        ILogger<EnrichContactPerson> logger)
    {
        _prospectRepo = prospectRepo;
        _aiClient = aiClient;
        _logger = logger;
    }

    public async Task<ContactPersonDto?> Handle(Guid contactId, string userId, CancellationToken ct = default)
    {
        _logger.LogInformation("Starting personal enrichment for contact {ContactId}", contactId);

        // 1. Fetch Contact with Prospect (for ownership validation)
        var contact = await _prospectRepo.GetContactPersonByIdAsync(contactId, ct);
        if (contact is null)
        {
            _logger.LogWarning("Contact {ContactId} not found", contactId);
            return null;
        }

        // 2. Fetch Prospect to validate ownership
        var prospect = await _prospectRepo.GetByIdAsync(contact.ProspectId, ct);
        if (prospect is null || prospect.OwnerId != userId)
        {
            _logger.LogWarning("Unauthorized access attempt for contact {ContactId} by user {UserId}", contactId, userId);
            return null;
        }

        _logger.LogInformation("Enriching contact: {Name} ({Title}) at {Company}", 
            contact.Name, contact.Title, prospect.Name);

        // 3. Build Research Prompt
        var researchPrompt = BuildResearchPrompt(contact, prospect);

        // 4. AI Research with Web Search
        var aiResponseText = await _aiClient.GenerateTextAsync(
            userInput: researchPrompt,
            systemPrompt: "You are an expert researcher specializing in B2B sales intelligence and personal background research for outreach purposes.",
            useWebSearch: true, // Enable web search for LinkedIn/news
            temperature: 0.3,
            maxOutputTokens: 1500,
            ct: ct
        );

        // 5. Parse AI Response
        var enrichmentData = ParseEnrichmentResponse(aiResponseText);

        // 6. Update Contact Entity
        contact.UpdateEnrichment(
            personalHooks: enrichmentData.PersonalHooks,
            personalNews: enrichmentData.PersonalNews,
            summary: enrichmentData.Summary
        );

        await _prospectRepo.UpdateContactPersonAsync(contact, ct);

        _logger.LogInformation("Successfully enriched contact {ContactId}", contactId);

        return ContactPersonDto.FromEntity(contact);
    }

    private static string BuildResearchPrompt(Domain.Entities.ContactPerson contact, Domain.Entities.Prospect prospect)
    {
        var promptBuilder = new System.Text.StringBuilder();
        
        promptBuilder.AppendLine($"Research this professional for B2B sales outreach purposes:");
        promptBuilder.AppendLine($"Name: {contact.Name}");
        
        if (!string.IsNullOrWhiteSpace(contact.Title))
            promptBuilder.AppendLine($"Title: {contact.Title}");
        
        promptBuilder.AppendLine($"Company: {prospect.Name}");
        
        if (!string.IsNullOrWhiteSpace(contact.LinkedInUrl))
            promptBuilder.AppendLine($"LinkedIn: {contact.LinkedInUrl}");
        
        if (!string.IsNullOrWhiteSpace(contact.Email))
            promptBuilder.AppendLine($"Email: {contact.Email}");
        
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("Find and extract:");
        promptBuilder.AppendLine("1. PERSONAL_HOOKS: 3-5 specific talking points, interests, or conversation starters (e.g., recent posts, shared connections, hobbies, volunteer work)");
        promptBuilder.AppendLine("2. PERSONAL_NEWS: Recent career moves, achievements, publications, speaking engagements, or awards (last 6 months)");
        promptBuilder.AppendLine("3. SUMMARY: A concise 2-3 sentence professional background summary");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("Format as valid JSON:");
        promptBuilder.AppendLine(@"{
  ""personalHooks"": [""string""],
  ""personalNews"": [""string""],
  ""summary"": ""string""
}");

        return promptBuilder.ToString();
    }

    private EnrichmentData ParseEnrichmentResponse(string? aiMessage)
    {
        if (string.IsNullOrWhiteSpace(aiMessage))
        {
            _logger.LogWarning("AI response was null or empty");
            return new EnrichmentData(null, null, "Enrichment processing failed (empty response).");
        }

        try
        {
            var jsonText = aiMessage.Trim();
            
            _logger.LogInformation("Raw AI response (first 200 chars): {Response}", 
                jsonText.Length > 200 ? jsonText[..200] : jsonText);
            
            // Remove HTML tags if present (sometimes AI returns HTML wrapped JSON)
            if (jsonText.StartsWith("<") || jsonText.Contains("<html>", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("AI response appears to be HTML, attempting to extract JSON");
                // Try to find JSON within HTML
                var jsonStart = jsonText.IndexOf('{');
                var jsonEnd = jsonText.LastIndexOf('}');
                if (jsonStart >= 0 && jsonEnd > jsonStart)
                {
                    jsonText = jsonText.Substring(jsonStart, jsonEnd - jsonStart + 1);
                }
                else
                {
                    _logger.LogError("Could not find JSON in HTML response");
                    return new EnrichmentData(null, null, "AI returned HTML instead of JSON. Please try again.");
                }
            }
            
            // 1. Clean Markdown if present
            if (jsonText.Contains("```"))
            {
                // Remove everything before the first ``` and after the last ``` 
                // match anything like ```json or just ```
                var start = jsonText.IndexOf("```");
                var end = jsonText.LastIndexOf("```");
                if (end > start)
                {
                    // Update jsonText to content inside the backticks
                    // We need to account for the first line being ```json maybe
                    var content = jsonText.Substring(start + 3, end - start - 3);
                    if (content.StartsWith("json", StringComparison.OrdinalIgnoreCase))
                    {
                        content = content.Substring(4);
                    }
                    jsonText = content.Trim();
                }
            }

            // 2. Extract JSON payload (greedy)
            var firstBrace = jsonText.IndexOf('{');
            var lastBrace = jsonText.LastIndexOf('}');
            
            if (firstBrace >= 0 && lastBrace > firstBrace)
            {
                jsonText = jsonText.Substring(firstBrace, lastBrace - firstBrace + 1);
            }
            else
            {
                _logger.LogWarning("No JSON braces found in response. Using raw response as summary.");
                // Fallback: If AI didn't return JSON, it probably returned a text explanation. Use that as summary.
                return new EnrichmentData(null, null, aiMessage ?? "Research completed but returned no structured data.");
            }

            // Ensure it starts with { or [
            // (Only relevant if we attempted extraction above)
            if (!jsonText.StartsWith("{") && !jsonText.StartsWith("["))
            {
                 // This should technically be unreachable if extraction worked, but good for safety
                _logger.LogWarning("Extracted text doesn't start with valid JSON. First char: {FirstChar}", 
                    jsonText.Length > 0 ? jsonText[0] : '?');
                return new EnrichmentData(null, null, aiMessage ?? "AI response was not in expected JSON format.");
            }

            // Parse JSON
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var parsed = JsonSerializer.Deserialize<EnrichmentResponseDto>(jsonText, options);

            if (parsed == null)
            {
                _logger.LogWarning("Failed to deserialize enrichment response to DTO");
                return new EnrichmentData(null, null, "Failed to parse AI response.");
            }

            return new EnrichmentData(parsed.PersonalHooks, parsed.PersonalNews, parsed.Summary ?? "No summary available.");
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON parsing failed.");
            // Fallback on JSON error too
            return new EnrichmentData(null, null, $"Analysis failed to format correctly. Raw result: {aiMessage}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse enrichment response");
            return new EnrichmentData(null, null, $"Parsing error: {ex.Message}");
        }
    }

    private record EnrichmentData(List<string>? PersonalHooks, List<string>? PersonalNews, string Summary);
    
    private record EnrichmentResponseDto(
        List<string>? PersonalHooks,
        List<string>? PersonalNews,
        string? Summary
    );
}
