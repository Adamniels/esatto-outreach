using Esatto.Outreach.Application.Abstractions;
using Esatto.Outreach.Application.DTOs;
using Esatto.Outreach.Domain.Entities;
using Esatto.Outreach.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Esatto.Outreach.Application.UseCases.Batch;

/// <summary>
/// Batch operation to generate soft company data for multiple prospects
/// Processes up to 5 prospects concurrently to avoid overwhelming AI APIs
/// </summary>
public sealed class GenerateSoftDataBatch
{
    private readonly ISoftCompanyDataRepository _dataRepo;
    private readonly IProspectRepository _prospectRepo;
    private readonly IResearchServiceFactory _researchFactory;
    private readonly ILogger<GenerateSoftDataBatch> _logger;

    // Semaphore to limit concurrent AI API calls (max 5)
    private static readonly SemaphoreSlim _semaphore = new(5, 5);

    public GenerateSoftDataBatch(
        ISoftCompanyDataRepository dataRepo,
        IProspectRepository prospectRepo,
        IResearchServiceFactory researchFactory,
        ILogger<GenerateSoftDataBatch> logger)
    {
        _dataRepo = dataRepo;
        _prospectRepo = prospectRepo;
        _researchFactory = researchFactory;
        _logger = logger;
    }

    /// <summary>
    /// Generate soft company data for multiple prospects
    /// </summary>
    /// <param name="prospectIds">List of prospect IDs to process</param>
    /// <param name="userId">User ID for ownership validation</param>
    /// <param name="provider">AI provider: "OpenAI", "Claude", or "Hybrid" (optional)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Batch operation result with successes and failures</returns>
    public async Task<BatchOperationResultDto<SoftCompanyDataDto>> Handle(
        List<Guid> prospectIds,
        string userId,
        string? provider = null,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Starting batch soft data generation for {Count} prospects by user {UserId} with provider {Provider}",
            prospectIds.Count, userId, provider ?? "configured");

        var results = new BatchOperationResultDto<SoftCompanyDataDto>();

        // 1. Validate ownership of ALL prospects upfront
        var prospects = await _prospectRepo.GetByIdsAsync(prospectIds, ct);
        
        var unauthorized = prospects.Where(p => p.OwnerId != userId).ToList();
        if (unauthorized.Any())
        {
            var unauthorizedIds = string.Join(", ", unauthorized.Select(p => p.Id));
            _logger.LogWarning("User {UserId} attempted to access {Count} unauthorized prospects: {ProspectIds}",
                userId, unauthorized.Count, unauthorizedIds);
            throw new UnauthorizedAccessException(
                $"{unauthorized.Count} prospect(s) do not belong to user. Access denied.");
        }

        var notFound = prospectIds.Except(prospects.Select(p => p.Id)).ToList();
        foreach (var missingId in notFound)
        {
            results.Failures.Add(new FailureResult(missingId, "Prospect not found"));
        }

        // 2. Get research service based on provider
        var researchService = string.IsNullOrWhiteSpace(provider)
            ? _researchFactory.GetResearchService()
            : _researchFactory.GetResearchService(provider);

        // 3. Process each prospect with concurrency control
        var tasks = prospects.Select(prospect => 
            ProcessProspectAsync(prospect, researchService, ct));

        var processResults = await Task.WhenAll(tasks);

        // 4. Collect results
        foreach (var result in processResults)
        {
            if (result.Success)
            {
                results.Successes.Add(new SuccessResult<SoftCompanyDataDto>(result.ProspectId, result.Data!));
            }
            else
            {
                results.Failures.Add(new FailureResult(result.ProspectId, result.ErrorMessage!));
            }
        }

        _logger.LogInformation("Batch soft data generation completed: {SuccessCount} succeeded, {FailureCount} failed",
            results.SuccessCount, results.FailureCount);

        return results;
    }

    private async Task<ProcessResult> ProcessProspectAsync(
        Prospect prospect,
        IResearchService researchService,
        CancellationToken ct)
    {
        await _semaphore.WaitAsync(ct);
        try
        {
            _logger.LogDebug("Processing prospect {ProspectId} ({CompanyName})",
                prospect.Id, prospect.CompanyName);

            // Generate research data
            var researchResult = await researchService.GenerateCompanyResearchAsync(
                prospect.CompanyName,
                prospect.Domain,
                ct);

            // Create or update SoftCompanyData entity
            SoftCompanyData softDataEntity;

            if (prospect.SoftCompanyDataId.HasValue)
            {
                // Update existing data
                var existingData = await _dataRepo.GetByIdAsync(prospect.SoftCompanyDataId.Value, ct);
                if (existingData != null)
                {
                    existingData.UpdateResearch(
                        hooksJson: researchResult.HooksJson,
                        recentEventsJson: researchResult.RecentEventsJson,
                        newsItemsJson: researchResult.NewsItemsJson,
                        socialActivityJson: researchResult.SocialActivityJson,
                        sourcesJson: researchResult.SourcesJson);

                    await _dataRepo.UpdateAsync(existingData, ct);
                    softDataEntity = existingData;

                    _logger.LogDebug("Updated existing soft data {SoftDataId} for prospect {ProspectId}",
                        existingData.Id, prospect.Id);
                }
                else
                {
                    // Old reference exists but entity missing, create new
                    softDataEntity = SoftCompanyData.Create(
                        prospectId: prospect.Id,
                        hooksJson: researchResult.HooksJson,
                        recentEventsJson: researchResult.RecentEventsJson,
                        newsItemsJson: researchResult.NewsItemsJson,
                        socialActivityJson: researchResult.SocialActivityJson,
                        sourcesJson: researchResult.SourcesJson);

                    await _dataRepo.AddAsync(softDataEntity, ct);
                    if (prospect.Status == ProspectStatus.New)
                    {
                        prospect.SetStatus(ProspectStatus.Researched);
                    }
                    prospect.LinkSoftCompanyData(softDataEntity.Id);
                    await _prospectRepo.UpdateAsync(prospect, ct);
                }
            }
            else
            {
                // Create new SoftCompanyData
                softDataEntity = SoftCompanyData.Create(
                    prospectId: prospect.Id,
                    hooksJson: researchResult.HooksJson,
                    recentEventsJson: researchResult.RecentEventsJson,
                    newsItemsJson: researchResult.NewsItemsJson,
                    socialActivityJson: researchResult.SocialActivityJson,
                    sourcesJson: researchResult.SourcesJson);

                await _dataRepo.AddAsync(softDataEntity, ct);
                if (prospect.Status == ProspectStatus.New)
                {
                    prospect.SetStatus(ProspectStatus.Researched);
                }
                prospect.LinkSoftCompanyData(softDataEntity.Id);
                await _prospectRepo.UpdateAsync(prospect, ct);

                _logger.LogDebug("Created new soft data {SoftDataId} for prospect {ProspectId}",
                    softDataEntity.Id, prospect.Id);
            }

            return new ProcessResult
            {
                Success = true,
                ProspectId = prospect.Id,
                Data = SoftCompanyDataDto.FromEntity(softDataEntity)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate soft data for prospect {ProspectId} ({CompanyName})",
                prospect.Id, prospect.CompanyName);

            return new ProcessResult
            {
                Success = false,
                ProspectId = prospect.Id,
                ErrorMessage = ex.Message
            };
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private class ProcessResult
    {
        public bool Success { get; init; }
        public Guid ProspectId { get; init; }
        public SoftCompanyDataDto? Data { get; init; }
        public string? ErrorMessage { get; init; }
    }
}
