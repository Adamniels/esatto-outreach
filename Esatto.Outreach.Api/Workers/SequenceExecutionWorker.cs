using Esatto.Outreach.Application.UseCases.Sequences;
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

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var orchestrator = scope.ServiceProvider.GetRequiredService<SequenceOrchestrator>();
                
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
