using System.Text.Json;
using Esatto.Outreach.Application.Abstractions;
using Esatto.Outreach.Application.DTOs;
using Microsoft.Extensions.Logging;

namespace Esatto.Outreach.Infrastructure.SoftDataCollection;

/// <summary>
/// Hybrid research service that runs OpenAI and Claude in parallel,
/// then aggregates and deduplicates the results.
/// </summary>
public sealed class HybridResearchService : IResearchService
{
    private readonly IResearchService _openAIService;
    private readonly IResearchService _claudeService;
    private readonly ILogger<HybridResearchService> _logger;

    public HybridResearchService(
        IEnumerable<IResearchService> researchServices,
        ILogger<HybridResearchService> logger)
    {
        _logger = logger;

        // Extract specific implementations from the collection
        // NOTE: This relies on DI registration order. In factory, we'll inject named services directly.
        var services = researchServices.ToList();
        
        _openAIService = services.FirstOrDefault(s => s.GetType().Name.Contains("OpenAI"))
            ?? throw new InvalidOperationException("OpenAI research service not found for Hybrid mode");
        
        _claudeService = services.FirstOrDefault(s => s.GetType().Name.Contains("Claude"))
            ?? throw new InvalidOperationException("Claude research service not found for Hybrid mode");
    }

    // Constructor for direct injection (used by factory)
    public HybridResearchService(
        IResearchService openAIService,
        IResearchService claudeService,
        ILogger<HybridResearchService> logger)
    {
        _openAIService = openAIService;
        _claudeService = claudeService;
        _logger = logger;
    }

    public async Task<SoftCompanyDataDto> GenerateCompanyResearchAsync(
        string companyName,
        string? domain,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Starting hybrid research for {CompanyName} using OpenAI + Claude in parallel", companyName);

        // Run both services in parallel
        var openAITask = RunWithErrorHandling(() => _openAIService.GenerateCompanyResearchAsync(companyName, domain, ct), "OpenAI");
        var claudeTask = RunWithErrorHandling(() => _claudeService.GenerateCompanyResearchAsync(companyName, domain, ct), "Claude");

        await Task.WhenAll(openAITask, claudeTask);

        var openAIResult = await openAITask;
        var claudeResult = await claudeTask;

        // Aggregate and deduplicate results
        var aggregated = AggregateResults(openAIResult, claudeResult, companyName);

        _logger.LogInformation("Hybrid research completed for {CompanyName}", companyName);

        return aggregated;
    }

    private async Task<SoftCompanyDataDto?> RunWithErrorHandling(
        Func<Task<SoftCompanyDataDto>> serviceCall,
        string providerName)
    {
        try
        {
            return await serviceCall();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Provider} research failed, continuing with other provider", providerName);
            return null;
        }
    }

    private SoftCompanyDataDto AggregateResults(
        SoftCompanyDataDto? openAIResult,
        SoftCompanyDataDto? claudeResult,
        string companyName)
    {
        // If both failed, throw
        if (openAIResult == null && claudeResult == null)
        {
            throw new InvalidOperationException($"Both OpenAI and Claude failed for {companyName}");
        }

        // If only one succeeded, return it
        if (openAIResult == null) return claudeResult!;
        if (claudeResult == null) return openAIResult;

        // Both succeeded - merge and deduplicate
        return new SoftCompanyDataDto(
            Id: Guid.Empty,
            ProspectId: Guid.Empty,
            HooksJson: MergeJsonArrays(openAIResult.HooksJson, claudeResult.HooksJson),
            RecentEventsJson: MergeJsonArrays(openAIResult.RecentEventsJson, claudeResult.RecentEventsJson),
            NewsItemsJson: MergeJsonArrays(openAIResult.NewsItemsJson, claudeResult.NewsItemsJson),
            SocialActivityJson: MergeJsonArrays(openAIResult.SocialActivityJson, claudeResult.SocialActivityJson),
            SourcesJson: MergeJsonArrays(openAIResult.SourcesJson, claudeResult.SourcesJson),
            ResearchedAt: DateTime.UtcNow,
            CreatedUtc: DateTime.UtcNow,
            UpdatedUtc: null
        );
    }

    private string? MergeJsonArrays(string? json1, string? json2)
    {
        if (string.IsNullOrWhiteSpace(json1) && string.IsNullOrWhiteSpace(json2))
            return null;

        if (string.IsNullOrWhiteSpace(json1)) return json2;
        if (string.IsNullOrWhiteSpace(json2)) return json1;

        try
        {
            var array1 = JsonSerializer.Deserialize<JsonElement[]>(json1);
            var array2 = JsonSerializer.Deserialize<JsonElement[]>(json2);

            if (array1 == null && array2 == null) return null;
            if (array1 == null) return json2;
            if (array2 == null) return json1;

            // Simple deduplication by converting to JSON strings and using HashSet
            var uniqueItems = new HashSet<string>();
            
            foreach (var item in array1)
            {
                uniqueItems.Add(item.GetRawText());
            }
            
            foreach (var item in array2)
            {
                uniqueItems.Add(item.GetRawText());
            }

            // Convert back to JSON array
            var mergedElements = uniqueItems
                .Select(json => JsonSerializer.Deserialize<JsonElement>(json))
                .ToArray();

            return JsonSerializer.Serialize(mergedElements);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to merge JSON arrays, returning first array");
            return json1;
        }
    }
}
