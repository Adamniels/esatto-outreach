using Esatto.Outreach.Domain.Entities.SequenceFeature;

namespace Esatto.Outreach.Application.Abstractions.Repositories;

public interface ISequenceRepository
{
    // Sequence Queries
    Task<Sequence?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Sequence?> GetByIdWithDetailsAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Sequence>> ListByOwnerAsync(string ownerId, CancellationToken ct = default);
    
    // Sequence Commands
    Task AddAsync(Sequence sequence, CancellationToken ct = default);
    Task AddStepAsync(SequenceStep step, CancellationToken ct = default);
    Task AddProspectAsync(SequenceProspect prospect, CancellationToken ct = default);
    Task UpdateAsync(Sequence sequence, CancellationToken ct = default);
    Task DeleteAsync(Sequence sequence, CancellationToken ct = default);
    
    // Prospect/Execution Queries
    /// <summary>
    /// Atomically claims up to <paramref name="batchSize"/> due enrollments for execution (lease persisted in the database).
    /// Returns the ids that were claimed; other workers will not receive the same rows until the lease expires.
    /// </summary>
    Task<IReadOnlyList<Guid>> ClaimDueActiveProspectsAsync(int batchSize, CancellationToken ct = default);
    Task<int> CountActiveProspectsForSequenceAsync(Guid sequenceId, CancellationToken ct = default);
    Task<IReadOnlyList<SequenceProspect>> GetPendingProspectsAsync(Guid sequenceId, int count, CancellationToken ct = default);
    Task<SequenceProspect?> GetProspectExecutionDetailsAsync(Guid sequenceProspectId, CancellationToken ct = default);
    Task<IReadOnlyList<Sequence>> ListActiveMultiSequencesAsync(CancellationToken ct = default);
    Task<int> ActivatePendingProspectsUpToLimitAsync(Guid sequenceId, int maxActiveProspects, CancellationToken ct = default);
}
