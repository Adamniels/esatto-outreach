using Esatto.Outreach.Application.Abstractions.Services;
using Esatto.Outreach.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Esatto.Outreach.Infrastructure.Services.StepExecutors;

public class EmailStepExecutor : IStepExecutor
{
    private readonly ILogger<EmailStepExecutor> _logger;

    public EmailStepExecutor(ILogger<EmailStepExecutor> logger)
    {
        _logger = logger;
    }

    public SequenceStepType StepType => SequenceStepType.Email;

    public Task ExecuteAsync(StepExecutionContext context, CancellationToken ct)
    {
        _logger.LogInformation("Dummy Execution: Sending Email for Sequence {SeqId} Step {StepId} to Prospect {ProspectId}", 
            context.Step.SequenceId, context.Step.Id, context.Prospect.Id);
            
        // TODO: Map to actual IEmailSender service
        
        return Task.CompletedTask;
    }
}
