using Esatto.Outreach.Application.Abstractions;
using Esatto.Outreach.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Esatto.Outreach.Infrastructure.Repositories;

public class HardCompanyDataRepository : IHardCompanyDataRepository
{
    private readonly OutreachDbContext _db;

    public HardCompanyDataRepository(OutreachDbContext db) => _db = db;

    public async Task<HardCompanyData?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _db.HardCompanyData.FirstOrDefaultAsync(d => d.Id == id, ct);

    public async Task<HardCompanyData> AddAsync(HardCompanyData data, CancellationToken ct = default)
    {
        await _db.HardCompanyData.AddAsync(data, ct);
        await _db.SaveChangesAsync(ct);
        return data;
    }

    public async Task UpdateAsync(HardCompanyData data, CancellationToken ct = default)
    {
        _db.HardCompanyData.Update(data);
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await _db.HardCompanyData.FirstOrDefaultAsync(d => d.Id == id, ct);
        if (entity is null) return;

        _db.HardCompanyData.Remove(entity);
        await _db.SaveChangesAsync(ct);
    }
}
