using Esatto.Outreach.Domain.Entities;
namespace Esatto.Outreach.Application.Abstractions;

public interface ICompanyRepository
{
    Task<Company?> GetByNameAsync(string name, CancellationToken ct = default);
    Task<Company?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(Company company, CancellationToken ct = default);
}