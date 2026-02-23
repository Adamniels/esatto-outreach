using Esatto.Outreach.Domain.Entities;

namespace Esatto.Outreach.Application.Abstractions;

public interface IInvitationRepository
{
    Task<Invitation?> GetByTokenAsync(string tokenHash, CancellationToken ct = default);
    Task AddAsync(Invitation invitation, CancellationToken ct = default);
    Task UpdateAsync(Invitation invitation, CancellationToken ct = default);
    Task<bool> MarkAsUsedAsync(Guid invitationId, CancellationToken ct = default);
}
