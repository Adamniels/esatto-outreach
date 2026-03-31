using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Application.Abstractions.Services;
using Microsoft.Extensions.Logging;

namespace Esatto.Outreach.Application.UseCases.Sequences;

// More
public class SequenceOrchestrator
{
    private readonly ISequenceRepository _repo;
    private readonly IEnumerable<IStepExecutor> _executors;
    private readonly ILogger<SequenceOrchestrator> _logger;

    public SequenceOrchestrator(
        ISequenceRepository repo,
        IEnumerable<IStepExecutor> executors,
        ILogger<SequenceOrchestrator> logger)
    {
        _repo = repo;
        _executors = executors;
        _logger = logger;
    }

    // TODO: want to make this more advanced later on, so it doesn't send too many requests at once.
    // but also make sure no sequence gets starved and never gets sent.
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

                var step = fullDetails.Sequence.SequenceSteps
                    .OrderBy(s => s.OrderIndex)
                    .ElementAtOrDefault(fullDetails.CurrentStepIndex);

                if (step == null)
                {
                    // No more steps, complete it
                    fullDetails.MarkSequenceCompleted();
                    await _repo.UpdateAsync(fullDetails.Sequence, ct);
                    continue;
                }

                var executor = _executors.FirstOrDefault(e => e.StepType == step.StepType);
                if (executor == null)
                    throw new InvalidOperationException($"No executor found for StepType {step.StepType}");

                var context = new StepExecutionContext(step, fullDetails, fullDetails.Prospect, fullDetails.Contact);
                await executor.ExecuteAsync(context, ct);

                // calculate next scheduled time
                var nextStepIndex = fullDetails.CurrentStepIndex + 1;
                var nextStep = fullDetails.Sequence.SequenceSteps
                    .OrderBy(s => s.OrderIndex)
                    .ElementAtOrDefault(nextStepIndex);

                if (nextStep == null)
                {
                    fullDetails.MarkSequenceCompleted();
                }
                else
                {
                    // Default to calculating DelayInDays exactly from now + roughly matching preferred time
                    var executeTime = DateTime.UtcNow.AddDays(nextStep.DelayInDays);
                    fullDetails.MarkStepCompleted(executeTime);
                }

                await _repo.UpdateAsync(fullDetails.Sequence, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute sequence step for SequenceProspect {Id}", p.Id);
                p.MarkFailed($"Execution error: {ex.Message}");
                // This is risky if Sequence was not explicitly retrieved identically. Need to ensure
                // we save safely. GetProspectExecutionDetailsAsync gives a tracked entity.
                await _repo.UpdateAsync(p.Sequence, ct);
            }
        }
    }
}
