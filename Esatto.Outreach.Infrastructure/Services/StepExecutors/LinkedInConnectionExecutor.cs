using Esatto.Outreach.Application.Abstractions.Services;
using Esatto.Outreach.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Esatto.Outreach.Infrastructure.Services.StepExecutors;

public class LinkedInConnectionExecutor : IStepExecutor
{
    private readonly ILogger<LinkedInConnectionExecutor> _logger;

    public LinkedInConnectionExecutor(ILogger<LinkedInConnectionExecutor> logger)
    {
        _logger = logger;
    }

    public SequenceStepType StepType => SequenceStepType.LinkedInConnectionRequest;

    public Task ExecuteAsync(StepExecutionContext context, CancellationToken ct)
    {
        _logger.LogInformation("Dummy Execution: Sending LinkedIn Connection Request for Sequence {SeqId} Step {StepId} to Prospect {ProspectId}", 
            context.Step.SequenceId, context.Step.Id, context.Prospect.Id);
            
        // TODO: Map to actual LinkedIn client service
        
        return Task.CompletedTask;
    }
}
