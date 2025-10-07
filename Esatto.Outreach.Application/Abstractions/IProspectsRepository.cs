using Esatto.Outreach.Domain.Entities;
using Esatto.Outreach.Domain.Enums;

namespace Esatto.Outreach.Application.Abstractions;

public interface IProspectRepository
{
    Task<Prospect?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Prospect>> ListAsync(CancellationToken ct = default);
    Task<Prospect> AddAsync(Prospect prospect, CancellationToken ct = default);
    Task UpdateAsync(Prospect prospect, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
