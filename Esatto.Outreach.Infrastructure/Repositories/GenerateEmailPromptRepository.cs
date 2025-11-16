using Esatto.Outreach.Application.Abstractions;
using Esatto.Outreach.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Esatto.Outreach.Infrastructure.Repositories;

public sealed class GenerateEmailPromptRepository : IGenerateEmailPromptRepository
{
    private readonly OutreachDbContext _db;

    public GenerateEmailPromptRepository(OutreachDbContext db) => _db = db;

    public async Task<GenerateEmailPrompt?> GetActiveAsync(CancellationToken ct = default)
        => await _db.GenerateEmailPrompts
            .FirstOrDefaultAsync(p => p.IsActive, ct);

    public async Task<GenerateEmailPrompt?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _db.GenerateEmailPrompts
            .FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<IReadOnlyList<GenerateEmailPrompt>> ListAllAsync(CancellationToken ct = default)
        => await _db.GenerateEmailPrompts
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

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await _db.GenerateEmailPrompts.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (entity is null) return;

        _db.GenerateEmailPrompts.Remove(entity);
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeactivateAllAsync(CancellationToken ct = default)
    {
        await _db.GenerateEmailPrompts
            .Where(p => p.IsActive)
            .ExecuteUpdateAsync(setters => 
                setters.SetProperty(p => p.IsActive, false)
                       .SetProperty(p => p.UpdatedUtc, DateTime.UtcNow), 
                ct);
    }
}
