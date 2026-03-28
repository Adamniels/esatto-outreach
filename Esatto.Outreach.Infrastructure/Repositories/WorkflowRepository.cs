using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Esatto.Outreach.Domain.Exceptions;

namespace Esatto.Outreach.Infrastructure.Repositories;

public class WorkflowRepository : IWorkflowRepository
{
    private readonly OutreachDbContext _context;

    public WorkflowRepository(OutreachDbContext context)
    {
        _context = context;
    }

    // Templates
    public async Task<List<WorkflowTemplate>> GetAllTemplatesAsync(CancellationToken ct)
    {
        return await _context.WorkflowTemplates
            .Include(t => t.Steps)
            .OrderBy(t => t.Name)
            .ToListAsync(ct);
    }

    public async Task<WorkflowTemplate?> GetTemplateByIdAsync(Guid id, CancellationToken ct)
    {
        return await _context.WorkflowTemplates
            .Include(t => t.Steps)
            .FirstOrDefaultAsync(t => t.Id == id, ct);
    }

    public async Task AddTemplateAsync(WorkflowTemplate template, CancellationToken ct)
    {
        await _context.WorkflowTemplates.AddAsync(template, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task UpdateTemplateAsync(WorkflowTemplate template, CancellationToken ct)
    {
        // 1. Force DETACH all WorkflowTemplateSteps currently tracked for this template.
        // This stops EF from trying to persist any generic "collection removed" events.
        var trackedSteps = _context.ChangeTracker.Entries<WorkflowTemplateStep>()
            .Where(e => e.Entity.WorkflowTemplateId == template.Id)
            .ToList();
            
        foreach (var entry in trackedSteps)
        {
            entry.State = EntityState.Detached;
        }

        // 2. Wipe existing steps in DB (Nuclear option for clean slate)
        await _context.WorkflowTemplateSteps
            .Where(s => s.WorkflowTemplateId == template.Id)
            .ExecuteDeleteAsync(ct);

        // 3. Explicitly ADD the new steps. 
        // Note: template.Steps contains the NEW steps (created in Service).
        // Since we detached old ones, and these are new instances, we just Add them.
        foreach (var step in template.Steps)
        {
            // Ensure they are linked (they should be, but safety first)
            // step.WorkflowTemplateId will be set by EF when adding to parent? 
            // Better to AddRange to the DbSet directly if ID is already set?
            // "WorkflowTemplate" entity is existing, so we can just update it.
            
            // Actually, since 'template' is tracked (or we act like it), 
            // and we modified 'Steps' collection with new objects,
            // we need to make sure EF sees them as Added.
             _context.Entry(step).State = EntityState.Added;
        }

        // 4. Ensure Template is Modified (it should be, but explicit is good)
        _context.Entry(template).State = EntityState.Modified;

        await _context.SaveChangesAsync(ct);
    }

    public async Task DeleteTemplateAsync(Guid id, CancellationToken ct)
    {
        var t = await GetTemplateByIdAsync(id, ct);
        if (t != null)
        {
            _context.WorkflowTemplates.Remove(t);
            await _context.SaveChangesAsync(ct);
        }
    }

    public async Task<WorkflowTemplate?> GetDefaultTemplateAsync(CancellationToken ct)
    {
        return await _context.WorkflowTemplates
            .Include(t => t.Steps)
            .FirstOrDefaultAsync(t => t.IsDefault, ct);
    }

    // Instances
    public async Task<WorkflowInstance?> GetInstanceByIdAsync(Guid id, CancellationToken ct)
    {
        var instance = await _context.WorkflowInstances
            .Include(i => i.Steps.OrderBy(s => s.OrderIndex))
            .Include(i => i.Prospect)
            .ThenInclude(p => p.ContactPersons)
            .FirstOrDefaultAsync(i => i.Id == id, ct);
        
        return instance;
    }

    public async Task<List<WorkflowInstance>> GetInstancesByProspectIdAsync(Guid prospectId, CancellationToken ct)
    {
         return await _context.WorkflowInstances
            .Include(i => i.Steps.OrderBy(s => s.OrderIndex))
            .Where(i => i.ProspectId == prospectId)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task AddInstanceAsync(WorkflowInstance instance, CancellationToken ct)
    {
        await _context.WorkflowInstances.AddAsync(instance, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task UpdateInstanceAsync(WorkflowInstance instance, CancellationToken ct)
    {
        // _context.WorkflowInstances.Update(instance); // EF Core Change Tracking handles this for tracked entities
        await _context.SaveChangesAsync(ct);
    }

    public async Task DeleteInstanceAsync(WorkflowInstance instance, CancellationToken ct)
    {
        _context.WorkflowInstances.Remove(instance);
        await _context.SaveChangesAsync(ct);
    }

    // Steps
    public async Task<WorkflowStep?> GetStepByIdAsync(Guid id, CancellationToken ct)
    {
        return await _context.WorkflowSteps
            .Include(s => s.WorkflowInstance)
            .ThenInclude(i => i.Prospect) // Needed for context
            .FirstOrDefaultAsync(s => s.Id == id, ct);
    }


    public async Task AddStepAsync(WorkflowStep step, CancellationToken ct)
    {
        await _context.WorkflowSteps.AddAsync(step, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task UpdateStepAsync(WorkflowStep step, CancellationToken ct)
    {
        try
        {
            _context.WorkflowSteps.Update(step);
            await _context.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new DomainConcurrencyException("Concurrency conflict in WorkflowStep update.", ex);
        }
    }

    public async Task DeleteStepAsync(WorkflowStep step, CancellationToken ct)
    {
        _context.WorkflowSteps.Remove(step);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<List<Guid>> GetDueStepsAsync(DateTime now, int limit, CancellationToken ct)
    {
        return await _context.WorkflowSteps
            .Where(s => s.Status == Domain.Enums.WorkflowStepStatus.Pending 
                        && s.RunAt <= now 
                        && s.WorkflowInstance.Status == Domain.Enums.WorkflowStatus.Active)
            .OrderBy(s => s.RunAt)
            .Take(limit)
            .Select(s => s.Id)
            .ToListAsync(ct);
    }

    public async Task<List<WorkflowStep>> GetStuckStepsAsync(DateTime olderThan, CancellationToken ct)
    {
        return await _context.WorkflowSteps
            .Where(s => s.Status == Domain.Enums.WorkflowStepStatus.Executing 
                        && s.UpdatedUtc < olderThan)
            .ToListAsync(ct);
    }

    public async Task SaveChangesAsync(CancellationToken ct)
    {
        await _context.SaveChangesAsync(ct);
    }
}
