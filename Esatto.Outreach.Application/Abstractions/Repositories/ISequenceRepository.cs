using Esatto.Outreach.Domain.Entities;

namespace Esatto.Outreach.Application.Abstractions.Repositories;

public interface ISequenceRepository
{
    Task<IReadOnlyList<Sequence>> ListByOwnerAsync(string ownerId, CancellationToken ct = default);
}
