using Esatto.Outreach.Domain.Enums;

namespace Esatto.Outreach.Application.DTOs.Workflows;

public record WorkflowTemplateDto(
    Guid Id,
    string Name,
    string? Description,
    bool IsDefault,
    List<WorkflowTemplateStepDto> Steps
);

public record WorkflowTemplateStepDto(
    WorkflowStepType Type,
    int DayOffset,
    string TimeOfDay, // "HH:mm"
    ContentGenerationStrategy? GenerationStrategy
);

public record CreateWorkflowTemplateRequest(
    string Name,
    string? Description,
    List<WorkflowTemplateStepDto> Steps
);

public record UpdateWorkflowTemplateRequest(
    string Name,
    string? Description,
    List<WorkflowTemplateStepDto> Steps
);

public record WorkflowInstanceDto(
    Guid Id,
    Guid ProspectId,
    WorkflowStatus Status,
    DateTime CreatedAt,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    List<WorkflowStepDto> Steps
);

public record WorkflowStepDto(
    Guid Id,
    WorkflowStepType Type,
    int OrderIndex,
    int DayOffset,
    string TimeOfDay, // "HH:mm"
    ContentGenerationStrategy? GenerationStrategy,
    DateTime? RunAt,
    WorkflowStepStatus Status,
    string? EmailSubject,
    string? BodyContent,
    string? FailureReason,
    int RetryCount
);

/// <summary>
/// Input DTO for defining steps when creating/updating workflow templates.
/// Used by use cases (replaces the old WorkflowTemplateStepDTO that lived in the service file).
/// </summary>
public record WorkflowTemplateStepInputDto(
    WorkflowStepType Type,
    int DayOffset,
    TimeSpan TimeOfDay,
    ContentGenerationStrategy? GenerationStrategy
);

// Endpoint request records (moved from WorkflowEndpoints.cs)
public record AddStepRequest(WorkflowStepType Type, int DayOffset, string TimeOfDay, ContentGenerationStrategy? GenerationStrategy);
public record UpdateStepContentRequest(string? Subject, string? Body);
public record UpdateStepConfigRequest(WorkflowStepType Type, int DayOffset, string TimeOfDay, ContentGenerationStrategy? GenerationStrategy);
public record UpdateTemplateStepRequest(WorkflowStepType Type, int DayOffset, string TimeOfDay, ContentGenerationStrategy? GenerationStrategy);
public record ActivateWorkflowRequest(string TimeZoneId = "UTC");

