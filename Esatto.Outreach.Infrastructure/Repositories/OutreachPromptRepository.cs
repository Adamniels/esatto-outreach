using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Domain.Entities;
using Esatto.Outreach.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Esatto.Outreach.Infrastructure.Repositories;

public sealed class OutreachPromptRepository : IOutreachPromptRepository
{
    private readonly OutreachDbContext _db;

    public OutreachPromptRepository(OutreachDbContext db) => _db = db;

    public async Task<OutreachPrompt?> GetActiveByUserIdAndTypeAsync(string userId, PromptType type, CancellationToken ct = default)
        => await _db.OutreachPrompts
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId && p.Type == type && p.IsActive, ct);

    public async Task<OutreachPrompt?> GetByIdAsync(Guid id, string userId, CancellationToken ct = default)
        => await _db.OutreachPrompts
            .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId, ct);

    public async Task<IReadOnlyList<OutreachPrompt>> ListByUserIdAsync(string userId, CancellationToken ct = default)
        => await _db.OutreachPrompts
            .AsNoTracking()
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.IsActive)
            .ThenByDescending(p => p.CreatedUtc)
            .ToListAsync(ct);

    public async Task<OutreachPrompt> AddAsync(OutreachPrompt prompt, CancellationToken ct = default)
    {
        await _db.OutreachPrompts.AddAsync(prompt, ct);
        return prompt;
    }

    public async Task UpdateAsync(OutreachPrompt prompt, CancellationToken ct = default)
    {
        _db.OutreachPrompts.Update(prompt);
    }

    public async Task DeleteAsync(Guid id, string userId, CancellationToken ct = default)
    {
        var entity = await _db.OutreachPrompts
            .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId, ct);
        if (entity is null) return;

        _db.OutreachPrompts.Remove(entity);
    }

    public async Task DeactivateAllForUserAndTypeAsync(string userId, PromptType type, CancellationToken ct = default)
    {
        var activePrompts = await _db.OutreachPrompts
            .Where(p => p.UserId == userId && p.Type == type && p.IsActive)
            .ToListAsync(ct);

        foreach (var prompt in activePrompts)
        {
            prompt.Deactivate();
        }
    }
}
