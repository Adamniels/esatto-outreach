using Esatto.Outreach.Application.Abstractions;
using Esatto.Outreach.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Esatto.Outreach.Infrastructure.Repositories;

public class CompanyRepository : ICompanyRepository
{
    private readonly OutreachDbContext _db;

    public CompanyRepository(OutreachDbContext db) => _db = db;

    public async Task<Company?> GetByNameAsync(string name, CancellationToken ct = default)
        => await _db.Companies
            .FirstOrDefaultAsync(c => c.Name == name, ct);

    public async Task<Company?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _db.Companies.FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task AddAsync(Company company, CancellationToken ct = default)
    {
        await _db.Companies.AddAsync(company, ct);
        await _db.SaveChangesAsync(ct);
    }
}
