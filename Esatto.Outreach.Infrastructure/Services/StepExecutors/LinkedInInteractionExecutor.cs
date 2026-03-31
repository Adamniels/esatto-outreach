using Esatto.Outreach.Application.Abstractions.Services;
using Esatto.Outreach.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Esatto.Outreach.Infrastructure.Services.StepExecutors;

public class LinkedInInteractionExecutor : IStepExecutor
{
    private readonly ILogger<LinkedInInteractionExecutor> _logger;

    public LinkedInInteractionExecutor(ILogger<LinkedInInteractionExecutor> logger)
    {
        _logger = logger;
    }

    public SequenceStepType StepType => SequenceStepType.LinkedInInteraction;

    public Task ExecuteAsync(StepExecutionContext context, CancellationToken ct)
    {
        _logger.LogInformation("Dummy Execution: Sending LinkedIn Interaction for Sequence {SeqId} Step {StepId} to Prospect {ProspectId}", 
            context.Step.SequenceId, context.Step.Id, context.Prospect.Id);
            
        // TODO: Map to actual LinkedIn client service
        
        return Task.CompletedTask;
    }
}
