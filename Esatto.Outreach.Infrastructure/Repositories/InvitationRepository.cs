using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Esatto.Outreach.Infrastructure.Repositories;

public sealed class InvitationRepository : IInvitationRepository
{
    private readonly OutreachDbContext _db;

    public InvitationRepository(OutreachDbContext db) => _db = db;

    public async Task<Invitation?> GetByTokenAsync(string tokenHash, CancellationToken ct = default)
        => await _db.Invitations
            .Include(i => i.Company)
            .FirstOrDefaultAsync(i => i.TokenHash == tokenHash, ct);

    public async Task AddAsync(Invitation invitation, CancellationToken ct = default)
    {
        await _db.Invitations.AddAsync(invitation, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Invitation invitation, CancellationToken ct = default)
    {
        _db.Invitations.Update(invitation);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<bool> MarkAsUsedAsync(Guid invitationId, CancellationToken ct = default)
    {
        var updatedCount = await _db.Invitations
            .Where(i => i.Id == invitationId && i.UsedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(i => i.UsedAt, DateTime.UtcNow), ct);
        return updatedCount > 0;
    }
}
