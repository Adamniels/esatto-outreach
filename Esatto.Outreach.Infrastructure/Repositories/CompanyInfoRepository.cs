using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Esatto.Outreach.Infrastructure.Repositories;

public class CompanyInfoRepository : ICompanyInfoRepository
{
    private readonly OutreachDbContext _db;

    public CompanyInfoRepository(OutreachDbContext db) => _db = db;

    public async Task<Guid?> GetCompanyIdByUserIdAsync(string userId, CancellationToken ct = default)
    {
        return await _db.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => u.CompanyId)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<CompanyInformation?> GetByCompanyIdAsync(Guid companyId, CancellationToken ct = default)
    {
        return await _db.CompanyInformations
            .AsNoTracking()
            .Include(x => x.Company)
            .FirstOrDefaultAsync(x => x.CompanyId == companyId, ct);
    }

    public async Task AddAsync(CompanyInformation info, CancellationToken ct = default)
    {
        _db.CompanyInformations.Add(info);
    }

    public async Task UpdateAsync(CompanyInformation info, CancellationToken ct = default)
    {
        _db.CompanyInformations.Update(info);
    }
}
