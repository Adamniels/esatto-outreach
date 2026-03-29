using Esatto.Outreach.Domain.Entities;

namespace Esatto.Outreach.Application.Abstractions.Repositories;

public interface ITrackRepository
{
    Task<IReadOnlyList<Track>> ListByOwnerAsync(string ownerId, CancellationToken ct = default);
}