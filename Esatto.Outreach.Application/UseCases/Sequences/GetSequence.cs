using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Application.DTOs.Sequence;

namespace Esatto.Outreach.Application.UseCases.Sequences;

public class GetSequence
{
    private readonly ISequenceRepository _repo;

    public GetSequence(ISequenceRepository repo)
    {
        _repo = repo;
    }

    public async Task<SequenceDetailsDto> Handle(Guid id, string userId, CancellationToken ct = default)
    {
        var sequence = await _repo.GetByIdWithDetailsAsync(id, ct);
        if (sequence == null)
            throw new KeyNotFoundException("Sequence not found");

        if (sequence.OwnerId != userId)
            throw new UnauthorizedAccessException("You don't have permission to access this sequence");

        return SequenceDetailsDto.FromEntity(sequence);
    }
}
