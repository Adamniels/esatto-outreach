using Esatto.Outreach.Domain.Common;
using Esatto.Outreach.Domain.Enums;

namespace Esatto.Outreach.Domain.Entities.SequenceFeature;

public class Sequence : Entity
{
    public string Title { get; private set; } = default!;
    public string? Description { get; private set; }

    public SequenceMode Mode { get; private set; }

    public SequenceStatus Status { get; private set; }
    public int CurrentBuilderStep { get; private set; } = 1;

    public string? OwnerId { get; private set; }
    public ApplicationUser? Owner { get; private set; }

    public List<SequenceStep> SequenceSteps { get; private set; } = new();
    public List<SequenceProspect> SequenceProspects { get; private set; } = new();

    public SequenceSettings Settings { get; private set; } = default!;
    public string? MultiEnrichment { get; private set; } // for multi mode

    protected Sequence() { } // For EF Core

    public static Sequence Create(
        string title,
        string? description,
        SequenceMode mode,
        string ownerId)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title is required", nameof(title));

        if (string.IsNullOrWhiteSpace(ownerId))
            throw new ArgumentException("OwnerId is required", nameof(ownerId));

        return new Sequence
        {
            Title = title.Trim(),
            Description = description?.Trim(),
            Mode = mode,
            Status = SequenceStatus.Setup,
            OwnerId = ownerId,
            Settings = SequenceSettings.CreateDefault(mode)
        };
    }

    public void UpdateDetails(string title, string? description)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title is required", nameof(title));

        Title = title.Trim();
        Description = description?.Trim();
        Touch();
    }

    public void SetStatus(SequenceStatus status)
    {
        Status = status;
        Touch();
    }

    public void AddStep(SequenceStep step)
    {
        if (Status != SequenceStatus.Draft && Status != SequenceStatus.Setup)
            throw new InvalidOperationException("Can only add steps to sequences in Draft or Setup status");

        SequenceSteps.Add(step);
        Touch();
    }

    public void UpdateBuilderStep(int step)
    {
        if (Status != SequenceStatus.Setup)
            throw new InvalidOperationException("Can only update builder step while in Setup status");

        CurrentBuilderStep = step;
        Touch();
    }

    public void CompleteSetup()
    {
        if (Status != SequenceStatus.Setup)
            throw new InvalidOperationException("Sequence is not in Setup status");

        Status = SequenceStatus.Draft;
        Touch();
    }
}
