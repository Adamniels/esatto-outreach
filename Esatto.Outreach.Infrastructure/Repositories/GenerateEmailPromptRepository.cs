using Esatto.Outreach.Application.Abstractions;
using Esatto.Outreach.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Esatto.Outreach.Infrastructure.Repositories;

public sealed class GenerateEmailPromptRepository : IGenerateEmailPromptRepository
{
    private readonly OutreachDbContext _db;

    public GenerateEmailPromptRepository(OutreachDbContext db) => _db = db;

    public async Task<GenerateEmailPrompt?> GetActiveByUserIdAsync(string userId, CancellationToken ct = default)
        => await _db.GenerateEmailPrompts
            .FirstOrDefaultAsync(p => p.UserId == userId && p.IsActive, ct);

    public async Task<GenerateEmailPrompt?> GetByIdAsync(Guid id, string userId, CancellationToken ct = default)
        => await _db.GenerateEmailPrompts
            .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId, ct);

    public async Task<IReadOnlyList<GenerateEmailPrompt>> ListByUserIdAsync(string userId, CancellationToken ct = default)
        => await _db.GenerateEmailPrompts
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.IsActive)
            .ThenByDescending(p => p.CreatedUtc)
            .ToListAsync(ct);

    public async Task<GenerateEmailPrompt> AddAsync(GenerateEmailPrompt prompt, CancellationToken ct = default)
    {
        await _db.GenerateEmailPrompts.AddAsync(prompt, ct);
        await _db.SaveChangesAsync(ct);
        return prompt;
    }

    public async Task UpdateAsync(GenerateEmailPrompt prompt, CancellationToken ct = default)
    {
        _db.GenerateEmailPrompts.Update(prompt);
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, string userId, CancellationToken ct = default)
    {
        var entity = await _db.GenerateEmailPrompts
            .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId, ct);
        if (entity is null) return;

        _db.GenerateEmailPrompts.Remove(entity);
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeactivateAllForUserAsync(string userId, CancellationToken ct = default)
    {
        var activePrompts = await _db.GenerateEmailPrompts
            .Where(p => p.UserId == userId && p.IsActive)
            .ToListAsync(ct);

        foreach (var prompt in activePrompts)
        {
            prompt.Deactivate();
        }

        await _db.SaveChangesAsync(ct);
    }
}
