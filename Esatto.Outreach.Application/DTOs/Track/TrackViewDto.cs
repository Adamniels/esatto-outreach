// Only what is needed to display the overview of the track, on the page
// where we can see all the tracks and their status. Then we can click on a track
// to see the details of the track.

namespace Esatto.Outreach.Application.DTOs.Track;

// NOTE: probably want to add more fields later on with actual data and information.
public record TrackViewDto(
    Guid Id,
    string Name,
    string Description,
    DateTime CreatedAt,
    DateTime UpdatedAt
);


