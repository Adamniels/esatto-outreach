using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Domain.Entities.SequenceFeature;
using Esatto.Outreach.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Npgsql;

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
            .AsNoTracking()
            .Include(s => s.SequenceProspects)
            .Where(s => s.OwnerId == ownerId)
            .OrderByDescending(s => s.CreatedUtc)
            .ToListAsync(ct);
    }

    public async Task AddAsync(Sequence sequence, CancellationToken ct = default)
    {
        await _context.Sequences.AddAsync(sequence, ct);
    }

    public async Task AddStepAsync(SequenceStep step, CancellationToken ct = default)
    {
        await _context.Set<SequenceStep>().AddAsync(step, ct);
    }

    public async Task AddProspectAsync(SequenceProspect prospect, CancellationToken ct = default)
    {
        await _context.Set<SequenceProspect>().AddAsync(prospect, ct);
    }

    public Task UpdateAsync(Sequence sequence, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        if (_context.Entry(sequence).State == EntityState.Detached)
        {
            _context.Sequences.Update(sequence);
        }

        return Task.CompletedTask;
    }

    public Task DeleteAsync(Sequence sequence, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        _context.Sequences.Remove(sequence);
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<Guid>> ClaimDueActiveProspectsAsync(int batchSize, CancellationToken ct = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(batchSize);

        var now = DateTime.UtcNow;
        var leaseUntilUtc = now.AddMinutes(2);

        // FOR UPDATE SKIP LOCKED: only one worker can claim a given row; UPDATE persists the lease before we return.
        await _context.Database.OpenConnectionAsync(ct);
        try
        {
            await using var cmd = _context.Database.GetDbConnection().CreateCommand();
            cmd.CommandText = """
                WITH picked AS MATERIALIZED (
                    SELECT sp."Id"
                    FROM "SequenceProspects" AS sp
                    INNER JOIN "Sequences" AS s ON sp."SequenceId" = s."Id"
                    WHERE sp."Status" = 'Active'
                      AND s."Status" = 'Active'
                      AND sp."NextStepScheduledAt" IS NOT NULL
                      AND sp."NextStepScheduledAt" <= @now
                    ORDER BY sp."NextStepScheduledAt" ASC NULLS LAST
                    FOR UPDATE OF sp SKIP LOCKED
                    LIMIT @batch_size
                ),
                updated AS (
                    UPDATE "SequenceProspects" AS sp
                    SET "NextStepScheduledAt" = @lease_until,
                        "UpdatedUtc" = @now,
                        "RowVersion" = gen_random_bytes(16)
                    FROM picked
                    WHERE sp."Id" = picked."Id"
                    RETURNING sp."Id"
                )
                SELECT "Id" FROM updated
                """;

            cmd.Parameters.Add(new NpgsqlParameter("now", NpgsqlTypes.NpgsqlDbType.TimestampTz) { Value = now });
            cmd.Parameters.Add(new NpgsqlParameter("lease_until", NpgsqlTypes.NpgsqlDbType.TimestampTz) { Value = leaseUntilUtc });
            cmd.Parameters.Add(new NpgsqlParameter("batch_size", batchSize));

            var ids = new List<Guid>();
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
                ids.Add(reader.GetGuid(0));

            return ids;
        }
        finally
        {
            await _context.Database.CloseConnectionAsync();
        }
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

    public async Task<IReadOnlyList<Sequence>> ListActiveMultiSequencesAsync(CancellationToken ct = default)
    {
        return await _context.Sequences
            .AsNoTracking()
            .Where(s => s.Status == SequenceStatus.Active && s.Mode == SequenceMode.Multi)
            .ToListAsync(ct);
    }

    public async Task<int> ActivatePendingProspectsUpToLimitAsync(Guid sequenceId, int maxActiveProspects, CancellationToken ct = default)
    {
        var currentlyActive = await CountActiveProspectsForSequenceAsync(sequenceId, ct);
        if (currentlyActive >= maxActiveProspects)
            return 0;

        var availableSlots = maxActiveProspects - currentlyActive;
        var pendingToActivate = await _context.SequenceProspects
            .Where(sp => sp.SequenceId == sequenceId && sp.Status == SequenceProspectStatus.Pending)
            .OrderBy(sp => sp.CreatedUtc)
            .Take(availableSlots)
            .ToListAsync(ct);

        foreach (var prospect in pendingToActivate)
        {
            prospect.Activate(DateTime.UtcNow);
        }

        return pendingToActivate.Count;
    }
}
