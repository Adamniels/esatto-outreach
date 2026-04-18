using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Application.Features.Sequences.Shared;

namespace Esatto.Outreach.Application.Features.Sequences.UpdateSequence;

public class UpdateSequenceCommandHandler
{
    private readonly ISequenceRepository _repo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly SequenceAccessCommandHandler _access;

    public UpdateSequenceCommandHandler(ISequenceRepository repo, IUnitOfWork unitOfWork, SequenceAccessCommandHandler access)
    {
        _repo = repo;
        _unitOfWork = unitOfWork;
        _access = access;
    }

    public async Task<SequenceViewDto> Handle(UpdateSequenceCommand command, string userId, CancellationToken ct = default)
    {
        var sequence = await _access.GetOwnedAsync(command.Id, userId, ct);

        sequence.ApplyMetadataUpdate(
            command.Title,
            command.Description,
            applySettings: command.Settings != null,
            enrichCompany: command.Settings?.EnrichCompany ?? true,
            enrichContact: command.Settings?.EnrichContact ?? true,
            researchSimilarities: command.Settings?.ResearchSimilarities ?? false,
            maxActiveProspectsPerDay: command.Settings?.MaxActiveProspectsPerDay ?? 20);

        await _repo.UpdateAsync(sequence, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return SequenceViewDto.FromEntity(sequence);
    }
}
