using Esatto.Outreach.Domain.Entities;
using Esatto.Outreach.Domain.Enums;

namespace Esatto.Outreach.Application.Abstractions;

public interface IOutreachPromptRepository
{
    Task<OutreachPrompt?> GetActiveByUserIdAndTypeAsync(string userId, PromptType type, CancellationToken ct = default);
    Task<OutreachPrompt?> GetByIdAsync(Guid id, string userId, CancellationToken ct = default);
    Task<IReadOnlyList<OutreachPrompt>> ListByUserIdAsync(string userId, CancellationToken ct = default);
    Task<OutreachPrompt> AddAsync(OutreachPrompt prompt, CancellationToken ct = default);
    Task UpdateAsync(OutreachPrompt prompt, CancellationToken ct = default);
    Task DeleteAsync(Guid id, string userId, CancellationToken ct = default);
    Task DeactivateAllForUserAndTypeAsync(string userId, PromptType type, CancellationToken ct = default);
}
