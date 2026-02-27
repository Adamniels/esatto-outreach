using Esatto.Outreach.Domain.Entities;
namespace Esatto.Outreach.Application.Abstractions;

public interface ICompanyInfoRepository
{
    Task<Guid?> GetCompanyIdByUserIdAsync(string userId, CancellationToken ct = default);
    Task<CompanyInformation?> GetByCompanyIdAsync(Guid companyId, CancellationToken ct = default);
    Task AddAsync(CompanyInformation info, CancellationToken ct = default);
    Task UpdateAsync(CompanyInformation info, CancellationToken ct = default);
}
