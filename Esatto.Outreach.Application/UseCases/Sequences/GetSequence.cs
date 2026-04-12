using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Application.DTOs.Sequence;

namespace Esatto.Outreach.Application.UseCases.Sequences;

public class GetSequence
{
    private readonly SequenceAccess _access;

    public GetSequence(SequenceAccess access)
    {
        _access = access;
    }

    public async Task<SequenceDetailsDto> Handle(Guid id, string userId, CancellationToken ct = default)
    {
        var sequence = await _access.GetOwnedWithDetailsForReadAsync(id, userId, ct);
        return SequenceDetailsDto.FromEntity(sequence);
    }
}
