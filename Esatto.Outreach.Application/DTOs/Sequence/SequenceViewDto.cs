// Only what is needed to display the overview of the sequence, on the page
// where we can see all the sequences and their status. Then we can click on a sequence
// to see the details of the sequence.

namespace Esatto.Outreach.Application.DTOs.Sequence;

// NOTE: probably want to add more fields later on with actual data and information.
public record SequenceViewDto(
    Guid Id,
    string Name,
    string Description,
    DateTime CreatedAt,
    DateTime UpdatedAt
);
