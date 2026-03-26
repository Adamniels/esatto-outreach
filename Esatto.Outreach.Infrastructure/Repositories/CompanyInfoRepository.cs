using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Application.Abstractions.Services;
using Esatto.Outreach.Application.Abstractions.Clients;
using Esatto.Outreach.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Esatto.Outreach.Infrastructure.Repositories;

public class CompanyInfoRepository : ICompanyInfoRepository
{
    private readonly OutreachDbContext _db;

    public CompanyInfoRepository(OutreachDbContext db) => _db = db;

    public async Task<Guid?> GetCompanyIdByUserIdAsync(string userId, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        return user?.CompanyId;
    }

    public async Task<CompanyInformation?> GetByCompanyIdAsync(Guid companyId, CancellationToken ct = default)
    {
        return await _db.CompanyInformations
            .Include(x => x.Company)
            .FirstOrDefaultAsync(x => x.CompanyId == companyId, ct);
    }

    public async Task AddAsync(CompanyInformation info, CancellationToken ct = default)
    {
        _db.CompanyInformations.Add(info);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(CompanyInformation info, CancellationToken ct = default)
    {
        _db.CompanyInformations.Update(info);
        await _db.SaveChangesAsync(ct);
    }
}
