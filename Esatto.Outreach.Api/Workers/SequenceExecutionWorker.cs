using Esatto.Outreach.Application.Features.Sequences.SequenceOrchestrator;
// using Microsoft.Extensions.Hosting;
// using Microsoft.Extensions.Logging;
// using Microsoft.Extensions.DependencyInjection;

namespace Esatto.Outreach.Api.Workers;

public class SequenceExecutionWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SequenceExecutionWorker> _logger;
    private readonly string? _leaderInstanceId;

    public SequenceExecutionWorker(IServiceProvider serviceProvider, ILogger<SequenceExecutionWorker> logger, IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _leaderInstanceId = configuration["BackgroundWorkers:LeaderInstanceId"];
    }

    // TODO: Consider implementing a more robust scheduling mechanism (e.g., Quartz.NET) if we need more control over execution times or retries.
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    if (!ShouldRunOnThisInstance())
                    {
                        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
                        continue;
                    }

                    using var scope = _serviceProvider.CreateScope();
                    var orchestrator = scope.ServiceProvider.GetRequiredService<SequenceOrchestratorCommandHandler>();

                    await orchestrator.ProcessDueStepsAsync(batchSize: 50, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred executing SequenceExecutionWorker.");
                }

                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Graceful shutdown.
        }
    }

    private bool ShouldRunOnThisInstance()
    {
        if (string.IsNullOrWhiteSpace(_leaderInstanceId))
            return true;

        var instanceId = Environment.GetEnvironmentVariable("HOSTNAME")
            ?? Environment.GetEnvironmentVariable("WEBSITE_INSTANCE_ID")
            ?? Environment.MachineName;

        return string.Equals(_leaderInstanceId, instanceId, StringComparison.OrdinalIgnoreCase);
    }
}
