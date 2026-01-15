using Esatto.Outreach.Domain.Entities;
using Esatto.Outreach.Domain.Enums;

namespace Esatto.Outreach.Application.Abstractions;

public interface IProspectRepository
{
    Task<Prospect?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Prospect?> GetByIdReadOnlyAsync(Guid id, CancellationToken ct = default);
    Task<Prospect?> GetByCapsuleIdAsync(long capsuleId, CancellationToken ct = default);
    Task<IReadOnlyList<Prospect>> GetByIdsAsync(List<Guid> ids, CancellationToken ct = default);
    Task<IReadOnlyList<Prospect>> ListAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Prospect>> ListByOwnerAsync(string ownerId, CancellationToken ct = default);
    Task<IReadOnlyList<Prospect>> ListPendingAsync(CancellationToken ct = default);
    Task<Prospect> AddAsync(Prospect prospect, CancellationToken ct = default);
    Task UpdateAsync(Prospect prospect, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    Task<ContactPerson?> GetContactPersonByIdAsync(Guid contactId, CancellationToken ct = default);
    Task AddContactPersonAsync(ContactPerson contact, CancellationToken ct = default);
    Task UpdateContactPersonAsync(ContactPerson contact, CancellationToken ct = default);
    Task DeleteContactPersonAsync(Guid contactId, CancellationToken ct = default);
}
