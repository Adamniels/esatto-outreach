using Esatto.Outreach.Domain.Entities;

namespace Esatto.Outreach.Application.Abstractions;

public interface IGenerateEmailPromptRepository
{
    Task<GenerateEmailPrompt?> GetActiveByUserIdAsync(string userId, CancellationToken ct = default);
    Task<GenerateEmailPrompt?> GetByIdAsync(Guid id, string userId, CancellationToken ct = default);
    Task<IReadOnlyList<GenerateEmailPrompt>> ListByUserIdAsync(string userId, CancellationToken ct = default);
    Task<GenerateEmailPrompt> AddAsync(GenerateEmailPrompt prompt, CancellationToken ct = default);
    Task UpdateAsync(GenerateEmailPrompt prompt, CancellationToken ct = default);
    Task DeleteAsync(Guid id, string userId, CancellationToken ct = default);
    Task DeactivateAllForUserAsync(string userId, CancellationToken ct = default);
}
