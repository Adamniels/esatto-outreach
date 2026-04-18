using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Application.Features.Sequences;

namespace Esatto.Outreach.Application.Features.Sequences;

public class ListSequencesQueryHandler
{
    private readonly ISequenceRepository _repo;
    public ListSequencesQueryHandler(ISequenceRepository repo) => _repo = repo;


    public async Task<IReadOnlyList<SequenceViewDto>> Handle(string userId, CancellationToken ct = default)
    {
        var list = await _repo.ListByOwnerAsync(userId, ct);
        return list.Select(SequenceViewDto.FromEntity).ToList();
    }
}
