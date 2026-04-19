using Esatto.Outreach.Application.Features.Sequences.ThrottleSequences;

namespace Esatto.Outreach.Api.Workers;


public class SequenceThrottleWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SequenceThrottleWorker> _logger;
    private readonly string? _leaderInstanceId;

    public SequenceThrottleWorker(IServiceProvider serviceProvider, ILogger<SequenceThrottleWorker> logger, IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _leaderInstanceId = configuration["BackgroundWorkers:LeaderInstanceId"];
    }

    // NOTE: Currenty this does not follow the maximum numbers "per day" but follows on maximun "at the moment"
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            // Give it a minute before starting the first loop
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    if (!ShouldRunOnThisInstance())
                    {
                        await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);
                        continue;
                    }

                    await using var scope = _serviceProvider.CreateAsyncScope();
                    var handler = scope.ServiceProvider.GetRequiredService<ThrottleSequencesCommandHandler>();
                    await handler.Handle(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred executing SequenceThrottleWorker.");
                }

                // Run every 2 minutes
                await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);
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
