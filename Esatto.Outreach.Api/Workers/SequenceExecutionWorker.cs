using Esatto.Outreach.Application.Features.Sequences.SequenceOrchestrator;
// using Microsoft.Extensions.Hosting;
// using Microsoft.Extensions.Logging;
// using Microsoft.Extensions.DependencyInjection;

namespace Esatto.Outreach.Api.Workers;

public class SequenceExecutionWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SequenceExecutionWorker> _logger;

    public SequenceExecutionWorker(IServiceProvider serviceProvider, ILogger<SequenceExecutionWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    // TODO: Consider implementing a more robust scheduling mechanism (e.g., Quartz.NET) if we need more control over execution times or retries.
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var orchestrator = scope.ServiceProvider.GetRequiredService<SequenceOrchestratorCommandHandler>();

                await orchestrator.ProcessDueStepsAsync(batchSize: 50, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred executing SequenceExecutionWorker.");
            }

            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }
}
