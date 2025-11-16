using Esatto.Outreach.Domain.Entities;

namespace Esatto.Outreach.Application.Abstractions;

public interface IGenerateEmailPromptRepository
{
    Task<GenerateEmailPrompt?> GetActiveAsync(CancellationToken ct = default);
    Task<GenerateEmailPrompt?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<GenerateEmailPrompt>> ListAllAsync(CancellationToken ct = default);
    Task<GenerateEmailPrompt> AddAsync(GenerateEmailPrompt prompt, CancellationToken ct = default);
    Task UpdateAsync(GenerateEmailPrompt prompt, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    Task DeactivateAllAsync(CancellationToken ct = default);
}
