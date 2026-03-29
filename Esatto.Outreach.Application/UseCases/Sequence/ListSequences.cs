using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Application.DTOs.Sequence;

namespace Esatto.Outreach.Application.UseCases.Sequence;

public class ListSequences
{
    private readonly ISequenceRepository _repo;
    public ListSequences(ISequenceRepository repo) => _repo = repo;


    public async Task<IReadOnlyList<SequenceViewDto>> Handle(string userId, CancellationToken ct = default)
    {
        var list = await _repo.ListByOwnerAsync(userId, ct);
        // NOTE: want to build this SequenceView in here becuase I dont want everything
        // when just showing it in a row.

        var viewList = new List<SequenceViewDto>();
        foreach (var sequence in list)
        {
            // TODO: build the SequenceViewDto in here
        }
        return viewList.ToList();
    }
}
