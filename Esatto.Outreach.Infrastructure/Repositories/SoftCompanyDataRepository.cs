using Esatto.Outreach.Application.Abstractions;
using Esatto.Outreach.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Esatto.Outreach.Infrastructure.Repositories;

public class SoftCompanyDataRepository : ISoftCompanyDataRepository
{
    private readonly OutreachDbContext _db;

    public SoftCompanyDataRepository(OutreachDbContext db) => _db = db;

    public async Task<SoftCompanyData?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _db.SoftCompanyData.FirstOrDefaultAsync(d => d.Id == id, ct);

    public async Task<SoftCompanyData> AddAsync(SoftCompanyData data, CancellationToken ct = default)
    {
        await _db.SoftCompanyData.AddAsync(data, ct);
        await _db.SaveChangesAsync(ct);
        return data;
    }

    public async Task UpdateAsync(SoftCompanyData data, CancellationToken ct = default)
    {
        _db.SoftCompanyData.Update(data);
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await _db.SoftCompanyData.FirstOrDefaultAsync(d => d.Id == id, ct);
        if (entity is null) return;

        _db.SoftCompanyData.Remove(entity);
        await _db.SaveChangesAsync(ct);
    }
}
