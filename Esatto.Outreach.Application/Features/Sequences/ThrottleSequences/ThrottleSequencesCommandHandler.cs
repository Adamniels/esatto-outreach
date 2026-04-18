using Esatto.Outreach.Application.Abstractions.Repositories;

namespace Esatto.Outreach.Application.Features.Sequences.ThrottleSequences;

public sealed class ThrottleSequencesCommandHandler
{
    private readonly ISequenceRepository _sequenceRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ThrottleSequencesCommandHandler(ISequenceRepository sequenceRepository, IUnitOfWork unitOfWork)
    {
        _sequenceRepository = sequenceRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(CancellationToken ct = default)
    {
        var multiSequences = await _sequenceRepository.ListActiveMultiSequencesAsync(ct);

        foreach (var sequence in multiSequences)
        {
            var throttleLimit = sequence.Settings.MaxActiveProspectsPerDay ?? 20;
            var activatedCount = await _sequenceRepository.ActivatePendingProspectsUpToLimitAsync(sequence.Id, throttleLimit, ct);

            if (activatedCount > 0)
            {
                await _unitOfWork.SaveChangesAsync(ct);
            }
        }
    }
}
