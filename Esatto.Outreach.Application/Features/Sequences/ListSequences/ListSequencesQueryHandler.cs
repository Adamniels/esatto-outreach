using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Application.Features.Sequences.Shared;

namespace Esatto.Outreach.Application.Features.Sequences.ListSequences;

public class ListSequencesQueryHandler
{
    private readonly ISequenceRepository _repo;
    public ListSequencesQueryHandler(ISequenceRepository repo) => _repo = repo;

    public async Task<IReadOnlyList<SequenceViewDto>> Handle(ListSequencesQuery query, string userId, CancellationToken ct = default)
    {
        var list = await _repo.ListByOwnerAsync(userId, ct);
        return list.Select(SequenceViewDto.FromEntity).ToList();
    }
}
