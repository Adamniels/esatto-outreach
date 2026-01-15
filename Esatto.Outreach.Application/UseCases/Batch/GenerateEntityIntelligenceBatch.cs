using Esatto.Outreach.Application.Abstractions;
using Esatto.Outreach.Application.DTOs;
using Esatto.Outreach.Application.UseCases.SoftDataCollection;
using Esatto.Outreach.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Esatto.Outreach.Application.UseCases.Batch;

/// <summary>
/// Batch operation to generate Entity Intelligence for multiple prospects.
/// </summary>
public sealed class GenerateEntityIntelligenceBatch
{
    private readonly GenerateEntityIntelligence _singleUseCase;
    private readonly IProspectRepository _prospectRepo;
    private readonly ILogger<GenerateEntityIntelligenceBatch> _logger;

    // Semaphore to limit concurrent AI/Scraping Calls (max 3)
    private static readonly SemaphoreSlim _semaphore = new(3, 3);

    public GenerateEntityIntelligenceBatch(
        GenerateEntityIntelligence singleUseCase,
        IProspectRepository prospectRepo,
        ILogger<GenerateEntityIntelligenceBatch> logger)
    {
        _singleUseCase = singleUseCase;
        _prospectRepo = prospectRepo;
        _logger = logger;
    }

    public async Task<BatchOperationResultDto<EntityIntelligenceDto>> Handle(
        List<Guid> prospectIds,
        string userId,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Starting batch Entity Intelligence for {Count} prospects by user {UserId}",
            prospectIds.Count, userId);

        var results = new BatchOperationResultDto<EntityIntelligenceDto>();

        // 1. Validate ownership
        var prospects = await _prospectRepo.GetByIdsAsync(prospectIds, ct);
        
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

        // 2. Process
        var tasks = prospects.Select(prospect => 
            ProcessProspectAsync(prospect, ct));

        var processResults = await Task.WhenAll(tasks);

        // 3. Collect
        foreach (var result in processResults)
        {
            if (result.Success)
                results.Successes.Add(new SuccessResult<EntityIntelligenceDto>(result.ProspectId, result.Data!));
            else
                results.Failures.Add(new FailureResult(result.ProspectId, result.ErrorMessage!));
        }

        return results;
    }

    private async Task<ProcessResult> ProcessProspectAsync(Prospect prospect, CancellationToken ct)
    {
        await _semaphore.WaitAsync(ct);
        try
        {
            var dto = await _singleUseCase.Handle(prospect.Id, ct);
            return new ProcessResult
            {
                Success = true,
                ProspectId = prospect.Id,
                Data = dto
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed enrichment for {ProspectId}", prospect.Id);
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
        public EntityIntelligenceDto? Data { get; init; }
        public string? ErrorMessage { get; init; }
    }
}
