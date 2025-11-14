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
            .Include(p => p.SoftCompanyData)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<IReadOnlyList<Prospect>> ListAsync(CancellationToken ct = default)
        => await _db.Prospects
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
        // EF trackar entiteten redan om den är hämtad via context
        _db.Prospects.Update(prospect);
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await _db.Prospects.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (entity is null) return;

        _db.Prospects.Remove(entity);
        await _db.SaveChangesAsync(ct);
    }
}
