using Esatto.Outreach.Domain.Entities;
namespace Esatto.Outreach.Application.Abstractions;

public interface IProjectCaseRepository
{
    Task<List<ProjectCase>> ListByCompanyIdAsync(Guid companyId, CancellationToken ct = default);
    Task<ProjectCase?> GetByIdAsync(Guid id, Guid companyId, CancellationToken ct = default);
    Task AddAsync(ProjectCase pc, CancellationToken ct = default);
    Task UpdateAsync(ProjectCase pc, CancellationToken ct = default);
    Task DeleteAsync(ProjectCase pc, CancellationToken ct = default);
}
