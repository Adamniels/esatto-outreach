using Esatto.Outreach.Domain.Entities;

namespace Esatto.Outreach.Application.Abstractions.Repositories;

public interface IWorkflowRepository
{
    // Templates
    Task<List<WorkflowTemplate>> GetAllTemplatesAsync(CancellationToken ct);
    Task<WorkflowTemplate?> GetTemplateByIdAsync(Guid id, CancellationToken ct);
    Task AddTemplateAsync(WorkflowTemplate template, CancellationToken ct);
    Task UpdateTemplateAsync(WorkflowTemplate template, CancellationToken ct);
    Task DeleteTemplateAsync(Guid id, CancellationToken ct);
    Task<WorkflowTemplate?> GetDefaultTemplateAsync(CancellationToken ct);

    // Instances
    Task<WorkflowInstance?> GetInstanceByIdAsync(Guid id, CancellationToken ct);
    Task<List<WorkflowInstance>> GetInstancesByProspectIdAsync(Guid prospectId, CancellationToken ct);
    Task AddInstanceAsync(WorkflowInstance instance, CancellationToken ct);
    Task UpdateInstanceAsync(WorkflowInstance instance, CancellationToken ct);
    Task DeleteInstanceAsync(WorkflowInstance instance, CancellationToken ct);
    
    // Steps
    Task<WorkflowStep?> GetStepByIdAsync(Guid id, CancellationToken ct);
    Task AddStepAsync(WorkflowStep step, CancellationToken ct);
    Task UpdateStepAsync(WorkflowStep step, CancellationToken ct);
    Task DeleteStepAsync(WorkflowStep step, CancellationToken ct);
    Task<List<Guid>> GetDueStepsAsync(DateTime now, int limit, CancellationToken ct);
    Task<List<WorkflowStep>> GetStuckStepsAsync(DateTime olderThan, CancellationToken ct);
    
    // Complex
    Task SaveChangesAsync(CancellationToken ct);
}
