using Esatto.Outreach.Application.Abstractions;
using Esatto.Outreach.Application.UseCases.Workflows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Esatto.Outreach.Api.Workers;

public class WorkflowExecutionWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<WorkflowExecutionWorker> _logger;
    private readonly TimeSpan _pollInterval = TimeSpan.FromSeconds(30);

    public WorkflowExecutionWorker(IServiceProvider serviceProvider, ILogger<WorkflowExecutionWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("WorkflowExecutionWorker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessDueStepsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in WorkflowExecutionWorker loop");
            }

            await Task.Delay(_pollInterval, stoppingToken);
        }
        
        _logger.LogInformation("WorkflowExecutionWorker stopping.");
    }

    private async Task ProcessDueStepsAsync(CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IWorkflowRepository>();
        var executionService = scope.ServiceProvider.GetRequiredService<StepExecutionService>();

        // Fetch IDs to keep transaction short/light or avoid tracking issues
        var dueStepIds = await repo.GetDueStepsAsync(DateTime.UtcNow, 50, ct);

        if (dueStepIds.Count > 0)
        {
            _logger.LogInformation("Found {Count} due steps", dueStepIds.Count);
        }

        foreach (var stepId in dueStepIds)
        {
            if (ct.IsCancellationRequested) break;
            
            // Execute each step in its own logical unit
            // StepExecutionService creates its own transaction logic (Claim -> Run -> Save)
            // It uses the same 'repo' instance from this scope.
            // If repository context lifetime is Scoped, it's shared.
            // This is fine as long as StepExecutionService doesn't error out the Context.
            // But EF Core contexts are not thread safe, we are sequential here.
            
            try
            {
                await executionService.ExecuteStepAsync(stepId, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute step {StepId}", stepId);
            }
        }
    }
}
