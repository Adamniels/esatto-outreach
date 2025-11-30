using Esatto.Outreach.Application.Abstractions;
using Esatto.Outreach.Application.DTOs;
using Esatto.Outreach.Domain.Entities;
using Esatto.Outreach.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Esatto.Outreach.Application.UseCases.Batch;

/// <summary>
/// Batch operation to generate emails for multiple prospects
/// If UseCollectedData type is selected and soft data is missing, it will automatically collect it first
/// Processes up to 5 prospects concurrently to avoid overwhelming AI APIs
/// </summary>
public sealed class GenerateEmailBatch
{
    private readonly IEmailContextBuilder _contextBuilder;
    private readonly IEmailGeneratorFactory _generatorFactory;
    private readonly IProspectRepository _prospectRepository;
    private readonly ISoftCompanyDataRepository _softDataRepository;
    private readonly IResearchServiceFactory _researchFactory;
    private readonly ILogger<GenerateEmailBatch> _logger;

    // Semaphore to limit concurrent AI API calls (max 5)
    private static readonly SemaphoreSlim _semaphore = new(5, 5);

    public GenerateEmailBatch(
        IEmailContextBuilder contextBuilder,
        IEmailGeneratorFactory generatorFactory,
        IProspectRepository prospectRepository,
        ISoftCompanyDataRepository softDataRepository,
        IResearchServiceFactory researchFactory,
        ILogger<GenerateEmailBatch> logger)
    {
        _contextBuilder = contextBuilder;
        _generatorFactory = generatorFactory;
        _prospectRepository = prospectRepository;
        _softDataRepository = softDataRepository;
        _researchFactory = researchFactory;
        _logger = logger;
    }

    /// <summary>
    /// Generate emails for multiple prospects with optional automatic soft data collection
    /// </summary>
    /// <param name="prospectIds">List of prospect IDs to process</param>
    /// <param name="userId">User ID for ownership validation</param>
    /// <param name="type">Email generator type: "WebSearch" or "UseCollectedData" (optional)</param>
    /// <param name="autoGenerateSoftData">If true and type is UseCollectedData, auto-generate missing soft data</param>
    /// <param name="softDataProvider">AI provider for soft data generation: "OpenAI", "Claude", or "Hybrid" (optional, default: Claude)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Batch operation result with successes and failures</returns>
    public async Task<BatchOperationResultDto<CustomEmailDraftDto>> Handle(
        List<Guid> prospectIds,
        string userId,
        string? type = null,
        bool autoGenerateSoftData = true,
        string? softDataProvider = "Claude",
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Starting batch email generation for {Count} prospects by user {UserId} with type {Type}, autoGenerate: {AutoGen}",
            prospectIds.Count, userId, type ?? "configured", autoGenerateSoftData);

        var results = new BatchOperationResultDto<CustomEmailDraftDto>();

        // 1. Validate ownership of ALL prospects upfront
        var prospects = await _prospectRepository.GetByIdsAsync(prospectIds, ct);
        
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

        // 2. Determine if we need soft data
        bool needsSoftData = !string.IsNullOrWhiteSpace(type) && 
            type.Equals("UseCollectedData", StringComparison.OrdinalIgnoreCase);

        // 3. If auto-generate is enabled and soft data is needed, collect it first for prospects that lack it
        if (needsSoftData && autoGenerateSoftData)
        {
            var prospectsNeedingSoftData = prospects
                .Where(p => !p.SoftCompanyDataId.HasValue)
                .ToList();

            if (prospectsNeedingSoftData.Any())
            {
                _logger.LogInformation(
                    "Auto-generating soft data for {Count} prospects without soft data using provider {Provider}",
                    prospectsNeedingSoftData.Count, softDataProvider ?? "Claude");

                await GenerateMissingSoftDataAsync(prospectsNeedingSoftData, softDataProvider, ct);

                // Reload prospects to get updated soft data references
                prospects = await _prospectRepository.GetByIdsAsync(prospectIds, ct);
                prospects = prospects.Where(p => prospectIds.Contains(p.Id)).ToList();
            }
        }

        // 4. Get email generator based on type
        var generator = string.IsNullOrWhiteSpace(type)
            ? _generatorFactory.GetGenerator()
            : _generatorFactory.GetGenerator(type);

        // 5. Process each prospect with concurrency control
        var tasks = prospects.Select(prospect => 
            ProcessProspectEmailAsync(prospect, userId, needsSoftData, generator, ct));

        var processResults = await Task.WhenAll(tasks);

        // 6. Collect results
        foreach (var result in processResults)
        {
            if (result.Success)
            {
                results.Successes.Add(new SuccessResult<CustomEmailDraftDto>(result.ProspectId, result.Data!));
            }
            else
            {
                results.Failures.Add(new FailureResult(result.ProspectId, result.ErrorMessage!));
            }
        }

        _logger.LogInformation("Batch email generation completed: {SuccessCount} succeeded, {FailureCount} failed",
            results.SuccessCount, results.FailureCount);

        return results;
    }

    private async Task GenerateMissingSoftDataAsync(
        List<Prospect> prospects,
        string? provider,
        CancellationToken ct)
    {
        var researchService = string.IsNullOrWhiteSpace(provider)
            ? _researchFactory.GetResearchService()
            : _researchFactory.GetResearchService(provider);

        var tasks = prospects.Select(prospect =>
            GenerateSoftDataForProspectAsync(prospect, researchService, ct));

        await Task.WhenAll(tasks);
    }

    private async Task GenerateSoftDataForProspectAsync(
        Prospect prospect,
        IResearchService researchService,
        CancellationToken ct)
    {
        await _semaphore.WaitAsync(ct);
        try
        {
            _logger.LogDebug("Auto-generating soft data for prospect {ProspectId} ({CompanyName})",
                prospect.Id, prospect.CompanyName);

            var researchResult = await researchService.GenerateCompanyResearchAsync(
                prospect.CompanyName,
                prospect.Domain,
                ct);

            var softDataEntity = SoftCompanyData.Create(
                prospectId: prospect.Id,
                hooksJson: researchResult.HooksJson,
                recentEventsJson: researchResult.RecentEventsJson,
                newsItemsJson: researchResult.NewsItemsJson,
                socialActivityJson: researchResult.SocialActivityJson,
                sourcesJson: researchResult.SourcesJson);

            await _softDataRepository.AddAsync(softDataEntity, ct);
            
            if (prospect.Status == ProspectStatus.New)
            {
                prospect.SetStatus(ProspectStatus.Researched);
            }
            prospect.LinkSoftCompanyData(softDataEntity.Id);
            await _prospectRepository.UpdateAsync(prospect, ct);

            _logger.LogDebug("Auto-generated soft data {SoftDataId} for prospect {ProspectId}",
                softDataEntity.Id, prospect.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to auto-generate soft data for prospect {ProspectId}", prospect.Id);
            // Continue with email generation even if soft data generation fails
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task<ProcessResult> ProcessProspectEmailAsync(
        Prospect prospect,
        string userId,
        bool includeSoftData,
        ICustomEmailGenerator generator,
        CancellationToken ct)
    {
        await _semaphore.WaitAsync(ct);
        try
        {
            _logger.LogDebug("Generating email for prospect {ProspectId} ({CompanyName})",
                prospect.Id, prospect.CompanyName);

            // Build context with all required data
            var context = await _contextBuilder.BuildContextAsync(prospect.Id, userId, includeSoftData, ct);

            // Generate email draft
            var draft = await generator.GenerateAsync(context, ct);

            // Update prospect with email draft
            prospect.UpdateBasics(
                mailTitle: draft.Title,
                mailBodyPlain: draft.BodyPlain,
                mailBodyHTML: draft.BodyHTML
            );

            await _prospectRepository.UpdateAsync(prospect, ct);

            _logger.LogDebug("Generated email for prospect {ProspectId}: {Title}",
                prospect.Id, draft.Title);

            return new ProcessResult
            {
                Success = true,
                ProspectId = prospect.Id,
                Data = new CustomEmailDraftDto(
                    Title: draft.Title,
                    BodyPlain: draft.BodyPlain,
                    BodyHTML: draft.BodyHTML
                )
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate email for prospect {ProspectId} ({CompanyName})",
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
        public CustomEmailDraftDto? Data { get; init; }
        public string? ErrorMessage { get; init; }
    }
}
