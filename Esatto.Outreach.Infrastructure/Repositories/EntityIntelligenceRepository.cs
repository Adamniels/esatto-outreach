using Esatto.Outreach.Application.Abstractions;
using Esatto.Outreach.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Esatto.Outreach.Infrastructure.Repositories;

public class EntityIntelligenceRepository : IEntityIntelligenceRepository
{
    private readonly OutreachDbContext _db;

    public EntityIntelligenceRepository(OutreachDbContext db) => _db = db;

    public async Task<EntityIntelligence?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _db.EntityIntelligences.FirstOrDefaultAsync(d => d.Id == id, ct);
    
    public async Task<EntityIntelligence?> GetByProspectIdAsync(Guid prospectId, CancellationToken ct = default)
        => await _db.EntityIntelligences.FirstOrDefaultAsync(d => d.ProspectId == prospectId, ct);

    public async Task<EntityIntelligence> AddAsync(EntityIntelligence data, CancellationToken ct = default)
    {
        await _db.EntityIntelligences.AddAsync(data, ct);
        await _db.SaveChangesAsync(ct);
        return data;
    }

    public async Task UpdateAsync(EntityIntelligence data, CancellationToken ct = default)
    {
        _db.EntityIntelligences.Update(data);
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await _db.EntityIntelligences.FirstOrDefaultAsync(d => d.Id == id, ct);
        if (entity is null) return;

        _db.EntityIntelligences.Remove(entity);
        await _db.SaveChangesAsync(ct);
    }
}
