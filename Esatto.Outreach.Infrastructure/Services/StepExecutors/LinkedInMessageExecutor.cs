using Esatto.Outreach.Application.Abstractions.Services;
using Esatto.Outreach.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Esatto.Outreach.Infrastructure.Services.StepExecutors;

public class LinkedInMessageExecutor : IStepExecutor
{
    private readonly ILogger<LinkedInMessageExecutor> _logger;

    public LinkedInMessageExecutor(ILogger<LinkedInMessageExecutor> logger)
    {
        _logger = logger;
    }

    public SequenceStepType StepType => SequenceStepType.LinkedInMessage;

    public Task ExecuteAsync(StepExecutionContext context, CancellationToken ct)
    {
        _logger.LogInformation("Dummy Execution: Sending LinkedIn Message for Sequence {SeqId} Step {StepId} to Prospect {ProspectId}", 
            context.Step.SequenceId, context.Step.Id, context.Prospect.Id);
            
        // TODO: Map to actual LinkedIn client service
        
        return Task.CompletedTask;
    }
}
