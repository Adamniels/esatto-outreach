using Esatto.Outreach.Domain.Entities;

namespace Esatto.Outreach.Application.Abstractions;

public interface IEntityIntelligenceRepository
{
    Task<EntityIntelligence?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<EntityIntelligence?> GetByProspectIdAsync(Guid prospectId, CancellationToken ct = default); // Helper, likely needed
    Task<EntityIntelligence> AddAsync(EntityIntelligence data, CancellationToken ct = default);
    Task UpdateAsync(EntityIntelligence data, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
