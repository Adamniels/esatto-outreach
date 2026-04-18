using Esatto.Outreach.Domain.Entities.SequenceFeature;
using Esatto.Outreach.Domain.Enums;

namespace Esatto.Outreach.Application.Features.Sequences.Shared;

public record SequenceSettingsDto(
    bool? EnrichCompany,
    bool? EnrichContact,
    bool? ResearchSimilarities,
    int? MaxActiveProspectsPerDay
)
{
    public static SequenceSettingsDto FromEntity(SequenceSettings entity) => new(
        entity.EnrichCompany,
        entity.EnrichContact,
        entity.ResearchSimilarities,
        entity.MaxActiveProspectsPerDay
    );
}

public record SequenceViewDto(
    Guid Id,
    string Title,
    string? Description,
    SequenceMode Mode,
    SequenceStatus Status,
    SequenceSettingsDto Settings,
    int ProspectCount,
    int CurrentBuilderStep,
    DateTime CreatedUtc,
    DateTime? UpdatedUtc
)
{
    public static SequenceViewDto FromEntity(Esatto.Outreach.Domain.Entities.SequenceFeature.Sequence entity) => new(
        entity.Id,
        entity.Title,
        entity.Description,
        entity.Mode,
        entity.Status,
        SequenceSettingsDto.FromEntity(entity.Settings),
        entity.SequenceProspects?.Count ?? 0,
        entity.CurrentBuilderStep,
        entity.CreatedUtc,
        entity.UpdatedUtc
    );
}

public record SequenceDetailsDto(
    Guid Id,
    string Title,
    string? Description,
    SequenceMode Mode,
    SequenceStatus Status,
    SequenceSettingsDto Settings,
    List<SequenceStepViewDto> Steps,
    List<SequenceProspectViewDto> Prospects,
    int CurrentBuilderStep,
    DateTime CreatedUtc,
    DateTime? UpdatedUtc
)
{
    public static SequenceDetailsDto FromEntity(Esatto.Outreach.Domain.Entities.SequenceFeature.Sequence entity) => new(
        entity.Id,
        entity.Title,
        entity.Description,
        entity.Mode,
        entity.Status,
        SequenceSettingsDto.FromEntity(entity.Settings),
        entity.SequenceSteps?.Select(SequenceStepViewDto.FromEntity).ToList() ?? [],
        entity.SequenceProspects?.Select(SequenceProspectViewDto.FromEntity).ToList() ?? [],
        entity.CurrentBuilderStep,
        entity.CreatedUtc,
        entity.UpdatedUtc
    );
}

public record SequenceStepViewDto(
    Guid Id,
    int OrderIndex,
    SequenceStepType StepType,
    int DelayInDays,
    TimeOfDay? TimeOfDayToRun,
    string? GeneratedSubject,
    string? GeneratedBody,
    OutreachGenerationType? GenerationType
)
{
    public static SequenceStepViewDto FromEntity(SequenceStep entity) => new(
        entity.Id,
        entity.OrderIndex,
        entity.StepType,
        entity.DelayInDays,
        entity.TimeOfDayToRun,
        entity.GeneratedSubject,
        entity.GeneratedBody,
        entity.GenerationType
    );
}

public record SequenceProspectViewDto(
    Guid Id,
    Guid ProspectId,
    string ProspectName,
    Guid ContactPersonId,
    string ContactPersonName,
    SequenceProspectStatus Status,
    int CurrentStepIndex,
    DateTime? NextStepScheduledAt,
    DateTime? LastStepExecutedAt,
    string? FailureReason
)
{
    public static SequenceProspectViewDto FromEntity(SequenceProspect entity) => new(
        entity.Id,
        entity.ProspectId,
        entity.Prospect?.Name ?? "Unknown",
        entity.ContactPersonId,
        entity.Contact?.Name ?? "Unknown",
        entity.Status,
        entity.CurrentStepIndex,
        entity.NextStepScheduledAt,
        entity.LastStepExecutedAt,
        entity.FailureReason
    );
}
