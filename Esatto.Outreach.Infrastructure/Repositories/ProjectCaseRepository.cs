using Esatto.Outreach.Application.Abstractions;
using Esatto.Outreach.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Esatto.Outreach.Infrastructure.Repositories;

public class ProjectCaseRepository : IProjectCaseRepository
{
    private readonly OutreachDbContext _db;

    public ProjectCaseRepository(OutreachDbContext db) => _db = db;

    public async Task<List<ProjectCase>> ListByCompanyIdAsync(Guid companyId, CancellationToken ct = default)
    {
        return await _db.ProjectCases
            .Where(x => x.CompanyId == companyId)
            .OrderByDescending(x => x.CreatedUtc)
            .ToListAsync(ct);
    }

    public async Task<ProjectCase?> GetByIdAsync(Guid id, Guid companyId, CancellationToken ct = default)
    {
        return await _db.ProjectCases.FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == companyId, ct);
    }

    public async Task AddAsync(ProjectCase pc, CancellationToken ct = default)
    {
        _db.ProjectCases.Add(pc);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(ProjectCase pc, CancellationToken ct = default)
    {
        _db.ProjectCases.Update(pc);
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(ProjectCase pc, CancellationToken ct = default)
    {
        _db.ProjectCases.Remove(pc);
        await _db.SaveChangesAsync(ct);
    }
}
