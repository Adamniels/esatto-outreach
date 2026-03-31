using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Domain.Entities.SequenceFeature;
using Esatto.Outreach.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Esatto.Outreach.Infrastructure.Repositories;

public class SequenceRepository : ISequenceRepository
{
    private readonly OutreachDbContext _context;

    public SequenceRepository(OutreachDbContext context)
    {
        _context = context;
    }

    public async Task<Sequence?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Sequences
            .FirstOrDefaultAsync(s => s.Id == id, ct);
    }

    public async Task<Sequence?> GetByIdWithDetailsAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Sequences
            .Include(s => s.SequenceSteps.OrderBy(st => st.OrderIndex))
            .Include(s => s.SequenceProspects)
                .ThenInclude(sp => sp.Prospect)
            .Include(s => s.SequenceProspects)
                .ThenInclude(sp => sp.Contact)
            .FirstOrDefaultAsync(s => s.Id == id, ct);
    }

    public async Task<IReadOnlyList<Sequence>> ListByOwnerAsync(string ownerId, CancellationToken ct = default)
    {
        return await _context.Sequences
            .Include(s => s.SequenceProspects)
            .Where(s => s.OwnerId == ownerId)
            .OrderByDescending(s => s.CreatedUtc)
            .ToListAsync(ct);
    }

    public async Task AddAsync(Sequence sequence, CancellationToken ct = default)
    {
        await _context.Sequences.AddAsync(sequence, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task AddStepAsync(SequenceStep step, CancellationToken ct = default)
    {
        await _context.Set<SequenceStep>().AddAsync(step, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task AddProspectAsync(SequenceProspect prospect, CancellationToken ct = default)
    {
        await _context.Set<SequenceProspect>().AddAsync(prospect, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Sequence sequence, CancellationToken ct = default)
    {
        if (_context.Entry(sequence).State == EntityState.Detached)
        {
            _context.Sequences.Update(sequence);
        }
        await _context.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Sequence sequence, CancellationToken ct = default)
    {
        _context.Sequences.Remove(sequence);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<SequenceProspect>> GetActiveProspectsDueForExecutionAsync(int batchSize, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        return await _context.SequenceProspects
            .Include(sp => sp.Sequence)
                .ThenInclude(s => s.SequenceSteps)
            .Where(sp => sp.Status == SequenceProspectStatus.Active 
                      && sp.Sequence.Status == SequenceStatus.Active
                      && sp.NextStepScheduledAt <= now)
            .OrderBy(sp => sp.NextStepScheduledAt)
            .Take(batchSize)
            .ToListAsync(ct);
    }

    public async Task<int> CountActiveProspectsForSequenceAsync(Guid sequenceId, CancellationToken ct = default)
    {
        return await _context.SequenceProspects
            .Where(sp => sp.SequenceId == sequenceId && sp.Status == SequenceProspectStatus.Active)
            .CountAsync(ct);
    }

    public async Task<IReadOnlyList<SequenceProspect>> GetPendingProspectsAsync(Guid sequenceId, int count, CancellationToken ct = default)
    {
        return await _context.SequenceProspects
            .Where(sp => sp.SequenceId == sequenceId && sp.Status == SequenceProspectStatus.Pending)
            .OrderBy(sp => sp.CreatedUtc) // oldest first
            .Take(count)
            .ToListAsync(ct);
    }

    public async Task<SequenceProspect?> GetProspectExecutionDetailsAsync(Guid sequenceProspectId, CancellationToken ct = default)
    {
        return await _context.SequenceProspects
            .Include(sp => sp.Sequence)
                .ThenInclude(s => s.SequenceSteps)
            .Include(sp => sp.Prospect)
            .Include(sp => sp.Contact)
            .FirstOrDefaultAsync(sp => sp.Id == sequenceProspectId, ct);
    }
}
