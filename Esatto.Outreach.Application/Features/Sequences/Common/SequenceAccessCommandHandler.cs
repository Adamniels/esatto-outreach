using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Domain.Entities.SequenceFeature;

namespace Esatto.Outreach.Application.Features.Sequences;

/// <summary>
/// Centralizes sequence load + ownership checks for command and query use cases.
/// </summary>
public sealed class SequenceAccessCommandHandler
{
    private readonly ISequenceRepository _repo;

    public SequenceAccessCommandHandler(ISequenceRepository repo)
    {
        _repo = repo;
    }

    public async Task<Sequence> GetOwnedWithDetailsAsync(Guid sequenceId, string userId, CancellationToken ct = default)
    {
        var sequence = await _repo.GetByIdWithDetailsAsync(sequenceId, ct);
        if (sequence == null)
            throw new KeyNotFoundException("Sequence not found");

        if (sequence.OwnerId != userId)
            throw new UnauthorizedAccessException("You don't have permission to modify this sequence");

        return sequence;
    }

    public async Task<Sequence> GetOwnedAsync(Guid sequenceId, string userId, CancellationToken ct = default)
    {
        var sequence = await _repo.GetByIdAsync(sequenceId, ct);
        if (sequence == null)
            throw new KeyNotFoundException("Sequence not found");

        if (sequence.OwnerId != userId)
            throw new UnauthorizedAccessException("You don't have permission to modify this sequence");

        return sequence;
    }

    /// <summary>For read-only access (e.g. GetSequenceQueryHandler) — same ownership semantics, different message.</summary>
    public async Task<Sequence> GetOwnedWithDetailsForReadAsync(Guid sequenceId, string userId, CancellationToken ct = default)
    {
        var sequence = await _repo.GetByIdWithDetailsAsync(sequenceId, ct);
        if (sequence == null)
            throw new KeyNotFoundException("Sequence not found");

        if (sequence.OwnerId != userId)
            throw new UnauthorizedAccessException("You don't have permission to access this sequence");

        return sequence;
    }
}
