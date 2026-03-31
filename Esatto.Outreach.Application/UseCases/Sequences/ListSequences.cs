using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Application.DTOs.Sequence;

namespace Esatto.Outreach.Application.UseCases.Sequences;

public class ListSequences
{
    private readonly ISequenceRepository _repo;
    public ListSequences(ISequenceRepository repo) => _repo = repo;


    public async Task<IReadOnlyList<SequenceViewDto>> Handle(string userId, CancellationToken ct = default)
    {
        var list = await _repo.ListByOwnerAsync(userId, ct);
        return list.Select(SequenceViewDto.FromEntity).ToList();
    }
}
