using Esatto.Outreach.Domain.Entities;

namespace Esatto.Outreach.Application.Abstractions;

public interface IHardCompanyDataRepository
{
    Task<HardCompanyData?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<HardCompanyData> AddAsync(HardCompanyData data, CancellationToken ct = default);
    Task UpdateAsync(HardCompanyData data, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
