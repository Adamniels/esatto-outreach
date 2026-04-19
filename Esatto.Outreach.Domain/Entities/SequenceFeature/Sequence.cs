using Esatto.Outreach.Domain.Common;
using Esatto.Outreach.Domain.Enums;

namespace Esatto.Outreach.Domain.Entities.SequenceFeature;

public class Sequence : Entity
{
    public string Title { get; private set; } = default!;
    public string? Description { get; private set; }

    public SequenceMode Mode { get; private set; }

    public SequenceStatus Status { get; private set; }
    public int CurrentBuilderStep { get; private set; } = 1; // Tracks progress in the builder, starts at 1 for new sequences 1.steps 2.prospects 3.settings -> overview

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

    /// <summary>
    /// Steps may change while the sequence is not executing. Active sequences must be paused first.
    /// </summary>
    public void EnsureSetupDraftOrPausedForStepMutations()
    {
        if (Status == SequenceStatus.Active)
            throw new InvalidOperationException(
                "Steps cannot be modified while the sequence is active. Pause the sequence first.");

        EnsureNotTerminal("Step changes");
    }

    /// <summary>
    /// Enrollment and sequence metadata (title, settings) stay editable whenever the sequence is not in a terminal state.
    /// </summary>
    public void EnsureNotTerminal(string actionDescription)
    {
        if (Status == SequenceStatus.Archived || Status == SequenceStatus.Completed || Status == SequenceStatus.Failed)
            throw new InvalidOperationException(
                $"{actionDescription} is not allowed because this sequence is archived, completed, or failed.");
    }

    /// <summary>
    /// Removes the aggregate when it is not running and not already finished.
    /// </summary>
    public void EnsureCanDelete()
    {
        if (Status == SequenceStatus.Active)
            throw new InvalidOperationException(
                "Cannot delete an active sequence. Pause or cancel it first.");

        EnsureNotTerminal("Deleting this sequence");
    }

    public void EnsureCanActivateFromDraftOrPaused()
    {
        if (Status != SequenceStatus.Draft && Status != SequenceStatus.Paused)
            throw new InvalidOperationException("Only draft or paused sequences can be activated.");
    }

    public void EnsureCanPause()
    {
        if (Status != SequenceStatus.Active)
            throw new InvalidOperationException("Only active sequences can be paused.");
    }

    public void EnsureCanCancel()
    {
        if (Status != SequenceStatus.Active && Status != SequenceStatus.Paused)
            throw new InvalidOperationException("Only active or paused sequences can be canceled.");
    }

    public void AddStep(SequenceStep step)
    {
        EnsureSetupDraftOrPausedForStepMutations();
        SequenceSteps.Add(step);
        Touch();
    }

    /// <returns>false if no step with <paramref name="stepId"/> exists.</returns>
    public bool RemoveStep(Guid stepId)
    {
        EnsureSetupDraftOrPausedForStepMutations();
        var step = SequenceSteps.FirstOrDefault(s => s.Id == stepId);
        if (step == null)
            return false;

        SequenceSteps.Remove(step);

        var remainingSteps = SequenceSteps.OrderBy(s => s.OrderIndex).ToList();
        for (int i = 0; i < remainingSteps.Count; i++)
            remainingSteps[i].UpdateOrder(i);

        Touch();
        return true;
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

    public int ComputeNextStepOrderIndex()
    {
        return SequenceSteps.Count == 0 ? 0 : SequenceSteps.Max(s => s.OrderIndex) + 1;
    }

    public void ReorderSteps(IReadOnlyList<Guid> stepIdsInOrder)
    {
        EnsureSetupDraftOrPausedForStepMutations();
        if (stepIdsInOrder == null)
            throw new ArgumentNullException(nameof(stepIdsInOrder));
        if (stepIdsInOrder.Count == 0)
            throw new ArgumentException("Step IDs are required.", nameof(stepIdsInOrder));

        var distinct = stepIdsInOrder.Distinct().ToList();
        if (distinct.Count != stepIdsInOrder.Count)
            throw new ArgumentException("Step IDs must be unique.", nameof(stepIdsInOrder));

        if (distinct.Count != SequenceSteps.Count)
            throw new ArgumentException(
                "The provided step IDs do not match the number of steps in the sequence.",
                nameof(stepIdsInOrder));

        for (int i = 0; i < distinct.Count; i++)
        {
            var step = SequenceSteps.FirstOrDefault(s => s.Id == distinct[i]);
            if (step == null)
                throw new ArgumentException(
                    $"Step with ID {distinct[i]} is not part of this sequence.",
                    nameof(stepIdsInOrder));

            step.UpdateOrder(i);
        }

        Touch();
    }

    /// <summary>
    /// Finishes the builder wizard: requires at least one step and one prospect, then moves Setup to Draft.
    /// </summary>
    public void CompleteWizard()
    {
        if (!SequenceSteps.Any())
            throw new InvalidOperationException(
                "Sequence must have at least one step before setup can be completed");

        if (!SequenceProspects.Any())
            throw new InvalidOperationException(
                "Sequence must have at least one prospect before setup can be completed");

        CompleteSetup();
    }

    public SequenceProspect EnrollProspect(Guid prospectId, Guid contactPersonId)
    {
        EnsureNotTerminal("Enrolling prospects");

        if (Mode == SequenceMode.Focused && SequenceProspects.Count >= 1)
            throw new InvalidOperationException("Focused sequences can only contain one prospect.");

        if (SequenceProspects.Any(sp => sp.ProspectId == prospectId))
            throw new InvalidOperationException("This prospect is already enrolled in this sequence.");

        var sp = SequenceProspect.Create(Id, prospectId, contactPersonId);
        SequenceProspects.Add(sp);
        Touch();
        return sp;
    }

    // NOTE: If a paused sequence get activated it will continue like nothing happened.
    // Is this what I want?
    public void Activate(DateTime utcNow)
    {
        EnsureCanActivateFromDraftOrPaused();

        var orderedSteps = SequenceSteps.OrderBy(s => s.OrderIndex).ToList();
        EnsureStepsReadyForActivation(orderedSteps);

        if (SequenceProspects.Count == 0)
            throw new InvalidOperationException("Cannot activate a sequence with no enclosed prospects.");

        if (Mode == SequenceMode.Focused && SequenceProspects.Count != 1)
            throw new InvalidOperationException("Focused sequence must contain exactly one prospect.");

        SetStatus(SequenceStatus.Active);

        foreach (var prospect in SequenceProspects.Where(p => p.Status == SequenceProspectStatus.Pending))
            prospect.Activate(utcNow);
    }

    public Guid GetBaselineProspectIdForContentGeneration()
    {
        if (Mode == SequenceMode.Focused)
        {
            if (SequenceProspects.Count != 1)
                throw new InvalidOperationException(
                    "Focused sequence must have exactly one prospect enrolled to generate content.");

            return SequenceProspects[0].ProspectId;
        }

        var first = SequenceProspects.OrderBy(sp => sp.CreatedUtc).FirstOrDefault();
        if (first == null)
            throw new InvalidOperationException(
                "Multi sequence must have at least one prospect enrolled to establish a prompt baseline.");

        return first.ProspectId;
    }

    /// <summary>Step at execution position (0-based), ordered by <see cref="SequenceStep.OrderIndex"/>.</summary>
    public SequenceStep? GetStepAtExecutionIndex(int executionIndex)
    {
        var ordered = SequenceSteps.OrderBy(s => s.OrderIndex).ToList();
        if (executionIndex < 0 || executionIndex >= ordered.Count)
            return null;

        return ordered[executionIndex];
    }

    private static void EnsureStepsReadyForActivation(IReadOnlyList<SequenceStep> orderedSteps)
    {
        if (orderedSteps.Count == 0)
            throw new InvalidOperationException("Cannot activate a sequence without steps.");

        foreach (var step in orderedSteps)
        {
            if (string.IsNullOrWhiteSpace(step.GeneratedBody))
                throw new InvalidOperationException(
                    "All steps must have generated content before activation.");

            if (step.StepType == SequenceStepType.Email && string.IsNullOrWhiteSpace(step.GeneratedSubject))
                throw new InvalidOperationException(
                    "All email steps must have a generated subject before activation.");
        }
    }

    /// <summary>Step that exists on this sequence and may be edited given current sequence status.</summary>
    public SequenceStep GetMutableStep(Guid stepId)
    {
        EnsureSetupDraftOrPausedForStepMutations();
        var step = SequenceSteps.FirstOrDefault(s => s.Id == stepId);
        if (step == null)
            throw new KeyNotFoundException("Step not found in this sequence");

        return step;
    }

    public void UpdateStepDefinition(
        Guid stepId,
        SequenceStepType stepType,
        int delayInDays,
        TimeOfDay? timeOfDayToRun,
        OutreachGenerationType? generationType)
    {
        var step = GetMutableStep(stepId);
        step.UpdateDetails(stepType, delayInDays, timeOfDayToRun, generationType);
        Touch();
    }

    public void UpdateStepGeneratedContent(Guid stepId, string? generatedSubject, string? generatedBody)
    {
        var step = GetMutableStep(stepId);
        step.SetGeneratedContent(generatedSubject, generatedBody);
        Touch();
    }

    /// <summary>Stores AI output on a step (same guards as manual content updates).</summary>
    public void ApplyGeneratedContentFromDraft(Guid stepId, string? subject, string bodyPlainOrHtml)
    {
        var step = GetMutableStep(stepId);
        step.SetGeneratedContent(subject, bodyPlainOrHtml);
        Touch();
    }

    public void RemoveStepOrThrow(Guid stepId)
    {
        if (!RemoveStep(stepId))
            throw new KeyNotFoundException("Step not found in this sequence");
    }

    public SequenceStep AddNewStep(
        SequenceStepType stepType,
        int delayInDays,
        TimeOfDay? timeOfDayToRun,
        OutreachGenerationType? generationType)
    {
        var step = SequenceStep.Create(
            Id,
            ComputeNextStepOrderIndex(),
            stepType,
            delayInDays,
            timeOfDayToRun,
            generationType);

        AddStep(step);
        return step;
    }

    public void RemoveEnrollment(Guid sequenceProspectId)
    {
        EnsureNotTerminal("Removing a prospect");

        var sp = SequenceProspects.FirstOrDefault(p => p.Id == sequenceProspectId);
        if (sp == null)
            throw new KeyNotFoundException("Prospect is not enrolled in this sequence");

        SequenceProspects.Remove(sp);
        Touch();
    }

    /// <summary>Updates title, description, and optionally settings. Enforces non-terminal status.</summary>
    public void ApplyMetadataUpdate(
        string title,
        string? description,
        bool applySettings,
        bool enrichCompany,
        bool enrichContact,
        bool researchSimilarities,
        int maxActiveProspectsPerDay)
    {
        EnsureNotTerminal("Updating the sequence");

        UpdateDetails(title, description);

        if (applySettings)
        {
            if (Mode == SequenceMode.Focused)
                Settings.UpdateFocusedSettings(enrichCompany, enrichContact);
            else
                Settings.UpdateMultiSettings(researchSimilarities, maxActiveProspectsPerDay);
        }

        Touch();
    }

    public void Pause()
    {
        EnsureCanPause();
        SetStatus(SequenceStatus.Paused);
    }

    public void CancelToArchived()
    {
        EnsureCanCancel();
        SetStatus(SequenceStatus.Archived);
    }
}
