using Esatto.Outreach.Application.Abstractions;
using Esatto.Outreach.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Esatto.Outreach.Infrastructure.Repositories;

public class ProspectRepository : IProspectRepository
{
    private readonly OutreachDbContext _db;

    public ProspectRepository(OutreachDbContext db) => _db = db;

    public async Task<Prospect?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _db.Prospects
            .Include(p => p.EntityIntelligence)
            .Include(p => p.ContactPersons)
            .Include(p => p.Owner)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<Prospect?> GetByIdReadOnlyAsync(Guid id, CancellationToken ct = default)
        => await _db.Prospects
            .AsNoTracking()
            .Include(p => p.EntityIntelligence)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<Prospect?> GetByCapsuleIdAsync(long capsuleId, CancellationToken ct = default)
        => await _db.Prospects
            .Include(p => p.EntityIntelligence)
            .Include(p => p.Owner)
            .FirstOrDefaultAsync(p => p.CapsuleId == capsuleId, ct);

    public async Task<IReadOnlyList<Prospect>> GetByIdsAsync(List<Guid> ids, CancellationToken ct = default)
        => await _db.Prospects
            .Include(p => p.EntityIntelligence)
            .Include(p => p.Owner)
            .Where(p => ids.Contains(p.Id))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Prospect>> ListAsync(CancellationToken ct = default)
        => await _db.Prospects
            .Include(p => p.EntityIntelligence)
            .OrderByDescending(p => p.CreatedUtc)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Prospect>> ListByOwnerAsync(string ownerId, CancellationToken ct = default)
        => await _db.Prospects
            .Include(p => p.EntityIntelligence)
            .Where(p => p.OwnerId == ownerId)
            .OrderByDescending(p => p.CreatedUtc)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Prospect>> ListPendingAsync(CancellationToken ct = default)
        => await _db.Prospects
            .Where(p => p.IsPending)
            .OrderByDescending(p => p.CreatedUtc)
            .ToListAsync(ct);

    public async Task<Prospect> AddAsync(Prospect prospect, CancellationToken ct = default)
    {
        await _db.Prospects.AddAsync(prospect, ct);
        await _db.SaveChangesAsync(ct);
        return prospect;
    }

    public async Task UpdateAsync(Prospect prospect, CancellationToken ct = default)
    {
        // EF tracks the entity if retrieved via context.
        // Explicit Update() forces the entire graph to Modified, which breaks adding new related entities (like ContactPerson)
        // because EF assumes they exist in DB and tries to UPDATE them instead of INSERT.
        if (_db.Entry(prospect).State == EntityState.Detached)
        {
            _db.Prospects.Update(prospect);
        }
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await _db.Prospects.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (entity is null) return;

        _db.Prospects.Remove(entity);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<ContactPerson?> GetContactPersonByIdAsync(Guid contactId, CancellationToken ct = default)
        => await _db.Set<ContactPerson>()
            .Include(c => c.Prospect)
            .FirstOrDefaultAsync(c => c.Id == contactId, ct);

    public async Task AddContactPersonAsync(ContactPerson contact, CancellationToken ct = default)
    {
        // Explicitly add to tracking as Added
        await _db.Set<ContactPerson>().AddAsync(contact, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateContactPersonAsync(ContactPerson contact, CancellationToken ct = default)
    {
        // Typically the contact is already attached if we fetched it, 
        // but to be safe we can check state or just Update.
        _db.Set<ContactPerson>().Update(contact);
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteContactPersonAsync(Guid contactId, CancellationToken ct = default)
    {
        var entity = await _db.Set<ContactPerson>().FirstOrDefaultAsync(c => c.Id == contactId, ct);
        if (entity != null)
        {
            _db.Set<ContactPerson>().Remove(entity);
            await _db.SaveChangesAsync(ct);
        }
    }
}
