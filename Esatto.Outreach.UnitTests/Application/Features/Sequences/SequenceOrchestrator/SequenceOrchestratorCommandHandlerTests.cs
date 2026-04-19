using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Application.Abstractions.Services;
using Esatto.Outreach.Application.Features.Sequences.SequenceOrchestrator;
using Esatto.Outreach.Domain.Entities.SequenceFeature;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Esatto.Outreach.UnitTests.Application.Features.Sequences.SequenceOrchestrator;

public class SequenceOrchestratorCommandHandlerTests
{
    private readonly ISequenceRepository _repo = Substitute.For<ISequenceRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IStepExecutor _executor = Substitute.For<IStepExecutor>();
    private readonly ILogger<SequenceOrchestratorCommandHandler> _logger =
        Substitute.For<ILogger<SequenceOrchestratorCommandHandler>>();

    private SequenceOrchestratorCommandHandler CreateHandler(IEnumerable<IStepExecutor>? executors = null)
    {
        return new SequenceOrchestratorCommandHandler(
            _repo,
            _unitOfWork,
            executors ?? new[] { _executor },
            _logger);
    }

    [Fact]
    public async Task ProcessDueStepsAsync_WhenNothingClaimed_DoesNotLoadProspects()
    {
        _repo.ClaimDueActiveProspectsAsync(50, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Guid>());

        var handler = CreateHandler();

        await handler.ProcessDueStepsAsync(50);

        await _repo.Received(1).ClaimDueActiveProspectsAsync(50, Arg.Any<CancellationToken>());
        await _repo.DidNotReceive()
            .GetProspectExecutionDetailsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessDueStepsAsync_WhenClaimedButProspectMissing_SkipsExecution()
    {
        var id = Guid.NewGuid();
        _repo.ClaimDueActiveProspectsAsync(50, Arg.Any<CancellationToken>())
            .Returns(new[] { id });
        _repo.GetProspectExecutionDetailsAsync(id, Arg.Any<CancellationToken>())
            .Returns((SequenceProspect?)null);

        var handler = CreateHandler();

        await handler.ProcessDueStepsAsync(50);

        await _executor.DidNotReceive()
            .ExecuteAsync(Arg.Any<StepExecutionContext>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
