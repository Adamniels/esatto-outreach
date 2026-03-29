using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Application.DTOs.Track;

namespace Esatto.Outreach.Application.UseCases.Track;

public class ListTracks
{
    private readonly ITrackRepository _repo;
    public ListTracks(ITrackRepository repo) => _repo = repo;


    public async Task<IReadOnlyList<TrackViewDto>> Handle(string userId, CancellationToken ct = default)
    {
        var list = await _repo.ListByOwnerAsync(userId, ct);
        // NOTE: want to build this TrackView in here becuase I dont want everything
        // when just showing it in a row.

        var viewList = new List<TrackViewDto>();
        foreach (var track in list)
        {
            // TODO: build the TrackViewDto in here
        }
        return viewList.ToList();
    }
}