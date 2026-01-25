using Esatto.Outreach.Application.Abstractions;
using Esatto.Outreach.Domain.Entities;
using Esatto.Outreach.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Esatto.Outreach.Api.Workers;

public class WorkflowCleanupWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<WorkflowCleanupWorker> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(60); // Check every hour
    private readonly TimeSpan _stuckThreshold = TimeSpan.FromHours(1); // Stuck if updating > 1 hour ago

    public WorkflowCleanupWorker(IServiceProvider serviceProvider, ILogger<WorkflowCleanupWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("WorkflowCleanupWorker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupStuckStepsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in WorkflowCleanupWorker loop");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }

        _logger.LogInformation("WorkflowCleanupWorker stopping.");
    }

    private async Task CleanupStuckStepsAsync(CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IWorkflowRepository>();
        
        var threshold = DateTime.UtcNow.Subtract(_stuckThreshold);
        
        var stuckSteps = await repo.GetStuckStepsAsync(threshold, ct);
        
        if (stuckSteps.Count > 0)
        {
            _logger.LogWarning("Found {Count} stuck steps (staying in Executing state for > 1 hour). Resetting to Pending.", stuckSteps.Count);
            
            foreach (var step in stuckSteps)
            {
                try 
                {
                   // Reset to Pending
                   step.Reset();
                   await repo.UpdateStepAsync(step, ct);
                }
                catch(Exception ex)
                {
                     _logger.LogError(ex, "Failed to reset stuck step {StepId}", step.Id);
                }
            }
        }
    }
}
