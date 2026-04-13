using Esatto.Outreach.Domain.Enums;
// using Esatto.Outreach.Application.Abstractions.Repositories;
// using Microsoft.Extensions.Hosting;
// using Microsoft.Extensions.Logging;
// using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Esatto.Outreach.Infrastructure; // Access to DbContext directly only for this edge-case wide scan if needed, or better, via repository

namespace Esatto.Outreach.Api.Workers;


public class SequenceThrottleWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SequenceThrottleWorker> _logger;

    public SequenceThrottleWorker(IServiceProvider serviceProvider, ILogger<SequenceThrottleWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    // NOTE: Currenty this does not follow the maximum numbers "per day" but follows on maximun "at the moment"
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Give it a minute before starting the first loop
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                // Since this needs to scan sequences in Multi mode, we will access DBContext directly for the worker,
                // or ideally via a specific method in the repository.
                var dbContext = scope.ServiceProvider.GetRequiredService<OutreachDbContext>();

                var multiSequences = await dbContext.Sequences
                    .Where(s => s.Status == SequenceStatus.Active && s.Mode == SequenceMode.Multi)
                    .ToListAsync(stoppingToken);

                foreach (var seq in multiSequences)
                {
                    var throttleLimit = seq.Settings.MaxActiveProspectsPerDay ?? 20;

                    var currentlyActive = await dbContext.SequenceProspects
                        .CountAsync(sp => sp.SequenceId == seq.Id && sp.Status == SequenceProspectStatus.Active, stoppingToken);

                    if (currentlyActive < throttleLimit)
                    {
                        var availableSlots = throttleLimit - currentlyActive;

                        var pendingToActivate = await dbContext.SequenceProspects
                            .Where(sp => sp.SequenceId == seq.Id && sp.Status == SequenceProspectStatus.Pending)
                            .OrderBy(sp => sp.CreatedUtc)
                            .Take(availableSlots)
                            .ToListAsync(stoppingToken);

                        foreach (var prospect in pendingToActivate)
                        {
                            prospect.Activate(DateTime.UtcNow);
                            _logger.LogInformation("Throttler activated Prospect {ProspectId} for Sequence {SeqId}", prospect.Id, seq.Id);
                        }

                        if (pendingToActivate.Any())
                        {
                            await dbContext.SaveChangesAsync(stoppingToken);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred executing SequenceThrottleWorker.");
            }

            // Run every 2 minutes
            await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);
        }
    }
}
