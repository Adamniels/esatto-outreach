using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Application.Features.Sequences;

namespace Esatto.Outreach.Application.Features.Sequences;

public class GetSequenceQueryHandler
{
    private readonly SequenceAccessCommandHandler _access;

    public GetSequenceQueryHandler(SequenceAccessCommandHandler access)
    {
        _access = access;
    }

    public async Task<SequenceDetailsDto> Handle(Guid id, string userId, CancellationToken ct = default)
    {
        var sequence = await _access.GetOwnedWithDetailsForReadAsync(id, userId, ct);
        return SequenceDetailsDto.FromEntity(sequence);
    }
}
