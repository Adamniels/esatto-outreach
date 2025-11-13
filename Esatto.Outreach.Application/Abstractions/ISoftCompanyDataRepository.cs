using Esatto.Outreach.Domain.Entities;

namespace Esatto.Outreach.Application.Abstractions;

public interface ISoftCompanyDataRepository
{
    Task<SoftCompanyData?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<SoftCompanyData> AddAsync(SoftCompanyData data, CancellationToken ct = default);
    Task UpdateAsync(SoftCompanyData data, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
