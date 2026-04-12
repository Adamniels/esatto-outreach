using Esatto.Outreach.Domain.Common;
using Esatto.Outreach.Domain.Enums;

namespace Esatto.Outreach.Domain.Entities.SequenceFeature;

public class SequenceStep : Entity
{
    public Guid SequenceId { get; private set; }
    public Sequence Sequence { get; private set; } = default!;

    public int OrderIndex { get; private set; }
    public SequenceStepType StepType { get; private set; }

    // Delay after previous step (in days)
    public int DelayInDays { get; private set; }

    // Preferred send time
    public TimeOfDay? TimeOfDayToRun { get; private set; }

    // Generated content based on template and prospect data
    public string? GeneratedSubject { get; private set; } // mainly for email
    public string? GeneratedBody { get; private set; } // for email and LinkedIn

    public OutreachGenerationType? GenerationType { get; private set; } // tells the generator which strategy to use

    protected SequenceStep() { } // EF Core

    public static SequenceStep Create(
        Guid sequenceId,
        int orderIndex,
        SequenceStepType stepType,
        int delayInDays,
        TimeOfDay? timeOfDayToRun = null,
        OutreachGenerationType? generationType = null)
    {
        if (delayInDays < 0)
            throw new ArgumentException("Delay in days cannot be negative", nameof(delayInDays));

        return new SequenceStep
        {
            SequenceId = sequenceId,
            OrderIndex = orderIndex,
            StepType = stepType,
            DelayInDays = delayInDays,
            TimeOfDayToRun = timeOfDayToRun,
            GenerationType = generationType
        };
    }

    public void UpdateDetails(
        SequenceStepType stepType,
        int delayInDays,
        TimeOfDay? timeOfDayToRun,
        OutreachGenerationType? generationType)
    {
        if (delayInDays < 0)
            throw new ArgumentException("Delay in days cannot be negative", nameof(delayInDays));

        StepType = stepType;
        DelayInDays = delayInDays;
        TimeOfDayToRun = timeOfDayToRun;
        GenerationType = generationType;
        Touch();
    }

    public void UpdateOrder(int newOrderIndex)
    {
        OrderIndex = newOrderIndex;
        Touch();
    }

    public void SetGeneratedContent(string? subject, string? body)
    {
        GeneratedSubject = subject;
        GeneratedBody = body;
        Touch();
    }

    public void ClearGeneratedContent()
    {
        GeneratedSubject = null;
        GeneratedBody = null;
        Touch();
    }

    public OutreachChannel GetOutreachChannel() => StepType switch
    {
        SequenceStepType.Email => OutreachChannel.Email,
        SequenceStepType.LinkedInMessage => OutreachChannel.LinkedIn,
        SequenceStepType.LinkedInConnectionRequest => OutreachChannel.LinkedIn,
        SequenceStepType.LinkedInInteraction => OutreachChannel.LinkedIn,
        _ => OutreachChannel.Email
    };
}
