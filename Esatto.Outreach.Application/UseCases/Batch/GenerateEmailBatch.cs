using Esatto.Outreach.Application.Abstractions;
using Esatto.Outreach.Application.DTOs;
using Esatto.Outreach.Domain.Entities;
using Esatto.Outreach.Domain.Enums;
using Esatto.Outreach.Application.UseCases.SoftDataCollection;
using Microsoft.Extensions.Logging;

namespace Esatto.Outreach.Application.UseCases.Batch;

/// <summary>
/// Batch operation to generate emails for multiple prospects
/// If UseCollectedData type is selected and Entity Intelligence is missing, it will automatically collect it first.
/// </summary>
public sealed class GenerateEmailBatch
{
    private readonly IEmailContextBuilder _contextBuilder;
    private readonly IEmailGeneratorFactory _generatorFactory;
    private readonly IProspectRepository _prospectRepository;
    private readonly GenerateEntityIntelligence _enrichmentUseCase;
    private readonly ILogger<GenerateEmailBatch> _logger;

    // Semaphore to limit concurrent AI API calls (max 5)
    private static readonly SemaphoreSlim _semaphore = new(5, 5);

    public GenerateEmailBatch(
        IEmailContextBuilder contextBuilder,
        IEmailGeneratorFactory generatorFactory,
        IProspectRepository prospectRepository,
        GenerateEntityIntelligence enrichmentUseCase,
        ILogger<GenerateEmailBatch> logger)
    {
        _contextBuilder = contextBuilder;
        _generatorFactory = generatorFactory;
        _prospectRepository = prospectRepository;
        _enrichmentUseCase = enrichmentUseCase;
        _logger = logger;
    }

    public async Task<BatchOperationResultDto<CustomEmailDraftDto>> Handle(
        List<Guid> prospectIds,
        string userId,
        string? type = null,
        bool autoGenerateSoftData = true,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Starting batch email generation for {Count} prospects by user {UserId} with type {Type}, autoGenerate: {AutoGen}",
            prospectIds.Count, userId, type ?? "configured", autoGenerateSoftData);

        var results = new BatchOperationResultDto<CustomEmailDraftDto>();

        // 1. Validate ownership
        var prospects = await _prospectRepository.GetByIdsAsync(prospectIds, ct);
        
        var unauthorized = prospects.Where(p => p.OwnerId != userId).ToList();
        if (unauthorized.Any())
        {
            throw new UnauthorizedAccessException($"{unauthorized.Count} prospect(s) do not belong to user.");
        }

        var notFound = prospectIds.Except(prospects.Select(p => p.Id)).ToList();
        foreach (var missingId in notFound)
        {
            results.Failures.Add(new FailureResult(missingId, "Prospect not found"));
        }

        // 2. Determine if we need enrichment data
        bool needsEnrichment = !string.IsNullOrWhiteSpace(type) && 
            type.Equals("UseCollectedData", StringComparison.OrdinalIgnoreCase);

        if (needsEnrichment && autoGenerateSoftData)
        {
            // Check for missing data OR broken links (ID set but entity null)
            var prospectsNeedingEnrichment = prospects
                .Where(p => p.EntityIntelligence == null) 
                .ToList();

            if (prospectsNeedingEnrichment.Any())
            {
                _logger.LogInformation("Auto-generating Entity Intelligence for {Count} prospects", prospectsNeedingEnrichment.Count);
                await GenerateMissingEnrichmentAsync(prospectsNeedingEnrichment, ct);

                // Reload prospects
                prospects = await _prospectRepository.GetByIdsAsync(prospectIds, ct);
                prospects = prospects.Where(p => prospectIds.Contains(p.Id)).ToList();
            }
        }

        // 4. Get generator
        var generator = string.IsNullOrWhiteSpace(type)
            ? _generatorFactory.GetGenerator()
            : _generatorFactory.GetGenerator(type);

        // 5. Process
        var tasks = prospects.Select(prospect => 
            ProcessProspectEmailAsync(prospect, userId, needsEnrichment, generator, ct));

        var processResults = await Task.WhenAll(tasks);

        // 6. Collect results
        foreach (var result in processResults)
        {
            if (result.Success)
                results.Successes.Add(new SuccessResult<CustomEmailDraftDto>(result.ProspectId, result.Data!));
            else
                results.Failures.Add(new FailureResult(result.ProspectId, result.ErrorMessage!));
        }

        return results;
    }

    private async Task GenerateMissingEnrichmentAsync(List<Prospect> prospects, CancellationToken ct)
    {
        var tasks = prospects.Select(async p => 
        {
            try 
            {
                // We don't need semaphore here if Handle already uses one or if we trust the underlying service.
                // But since GenerateEntityIntelligence doesn't have a semaphore, we might want one here OR rely on the concurrency limits of the callers.
                // For simplicity, we just call it. Parallelism is fine for scraping usually.
                await _enrichmentUseCase.Handle(p.Id, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to auto-generate enrichment for {ProspectId}", p.Id);
            }
        });

        await Task.WhenAll(tasks);
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
            var context = await _contextBuilder.BuildContextAsync(prospect.Id, userId, includeSoftData, ct);
            var draft = await generator.GenerateAsync(context, ct);

            prospect.UpdateBasics(
                mailTitle: draft.Title,
                mailBodyPlain: draft.BodyPlain,
                mailBodyHTML: draft.BodyHTML
            );

            await _prospectRepository.UpdateAsync(prospect, ct);

            return new ProcessResult
            {
                Success = true,
                ProspectId = prospect.Id,
                Data = new CustomEmailDraftDto(draft.Title, draft.BodyPlain, draft.BodyHTML)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate email for {ProspectId}", prospect.Id);
            return new ProcessResult { Success = false, ProspectId = prospect.Id, ErrorMessage = ex.Message };
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
