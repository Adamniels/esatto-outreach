using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Application.Abstractions.Services;
using Microsoft.Extensions.Logging;

namespace Esatto.Outreach.Application.Features.Sequences.SequenceOrchestrator;

public class SequenceOrchestratorCommandHandler
{
    private readonly ISequenceRepository _repo;
    private readonly IEnumerable<IStepExecutor> _executors;
    private readonly ILogger<SequenceOrchestratorCommandHandler> _logger;

    public SequenceOrchestratorCommandHandler(
        ISequenceRepository repo,
        IEnumerable<IStepExecutor> executors,
        ILogger<SequenceOrchestratorCommandHandler> logger)
    {
        _repo = repo;
        _executors = executors;
        _logger = logger;
    }

    public async Task ProcessDueStepsAsync(int batchSize, CancellationToken ct = default)
    {
        var dueProspects = await _repo.GetActiveProspectsDueForExecutionAsync(batchSize, ct);

        if (!dueProspects.Any()) return;

        foreach (var p in dueProspects)
        {
            try
            {
                var fullDetails = await _repo.GetProspectExecutionDetailsAsync(p.Id, ct);
                if (fullDetails == null) continue;

                var sequence = fullDetails.Sequence;

                if (fullDetails.TryCompleteIfNoCurrentStep(sequence))
                {
                    await _repo.UpdateAsync(sequence, ct);
                    continue;
                }

                var step = sequence.GetStepAtExecutionIndex(fullDetails.CurrentStepIndex)!;

                var executor = _executors.FirstOrDefault(e => e.StepType == step.StepType);
                if (executor == null)
                    throw new InvalidOperationException($"No executor found for StepType {step.StepType}");

                var context = new StepExecutionContext(step, fullDetails, fullDetails.Prospect, fullDetails.Contact);
                await executor.ExecuteAsync(context, ct);

                fullDetails.RecordSuccessfulStepAndScheduleNext(sequence, DateTime.UtcNow);

                await _repo.UpdateAsync(sequence, ct);
            }
            catch (Exception ex) when (ex.GetType().Name == "DbUpdateConcurrencyException")
            {
                _logger.LogWarning(ex, "Concurrency conflict for SequenceProspect {Id}; leaving for retry", p.Id);
            }
            catch (TimeoutException ex)
            {
                _logger.LogWarning(ex, "Timeout for SequenceProspect {Id}; leaving for retry", p.Id);
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                _logger.LogWarning("Unexpected operation cancellation for SequenceProspect {Id}; leaving for retry", p.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute sequence step for SequenceProspect {Id}", p.Id);
                p.MarkFailed($"Execution error: {ex.Message}");
                await _repo.UpdateAsync(p.Sequence, ct);
            }
        }
    }
}
