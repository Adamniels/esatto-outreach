using Esatto.Outreach.Domain.Entities.SequenceFeature;
using Esatto.Outreach.Domain.Enums;
using FluentAssertions;

namespace Esatto.Outreach.UnitTests.Domain.Entities;

public class SequenceTests
{
    private static Sequence NewFocused() =>
        Sequence.Create("Test", null, SequenceMode.Focused, "owner-1");

    private static Sequence NewMulti() =>
        Sequence.Create("Test", null, SequenceMode.Multi, "owner-1");

    [Fact]
    public void CompleteWizard_WithoutSteps_Throws()
    {
        var seq = NewFocused();
        var act = () => seq.CompleteWizard();
        act.Should().Throw<InvalidOperationException>().WithMessage("*at least one step*");
    }

    [Fact]
    public void CompleteWizard_WithStepButNoProspect_Throws()
    {
        var seq = NewFocused();
        seq.AddStep(SequenceStep.Create(seq.Id, 0, SequenceStepType.Email, 0));
        var act = () => seq.CompleteWizard();
        act.Should().Throw<InvalidOperationException>().WithMessage("*at least one prospect*");
    }

    [Fact]
    public void CompleteWizard_WhenValid_TransitionsToDraft()
    {
        var seq = NewFocused();
        seq.AddStep(SequenceStep.Create(seq.Id, 0, SequenceStepType.Email, 0));
        seq.EnrollProspect(Guid.NewGuid(), Guid.NewGuid());
        seq.CompleteWizard();
        seq.Status.Should().Be(SequenceStatus.Draft);
    }

    [Fact]
    public void Activate_EmailStepWithoutSubject_Throws()
    {
        var seq = NewFocused();
        seq.AddStep(SequenceStep.Create(seq.Id, 0, SequenceStepType.Email, 0));
        seq.EnrollProspect(Guid.NewGuid(), Guid.NewGuid());
        seq.CompleteWizard();
        seq.SequenceSteps[0].SetGeneratedContent(null, "body text");
        var act = () => seq.Activate(DateTime.UtcNow);
        act.Should().Throw<InvalidOperationException>().WithMessage("*subject*");
    }

    [Fact]
    public void Activate_WhenValid_SetsActiveAndActivatesPendingProspects()
    {
        var seq = NewFocused();
        seq.AddStep(SequenceStep.Create(seq.Id, 0, SequenceStepType.LinkedInMessage, 0));
        seq.EnrollProspect(Guid.NewGuid(), Guid.NewGuid());
        seq.CompleteWizard();
        seq.SequenceSteps[0].SetGeneratedContent(null, "msg");
        seq.Activate(DateTime.UtcNow);
        seq.Status.Should().Be(SequenceStatus.Active);
        seq.SequenceProspects[0].Status.Should().Be(SequenceProspectStatus.Active);
    }

    [Fact]
    public void ReorderSteps_WrongCount_ThrowsArgumentException()
    {
        var seq = NewFocused();
        var a = SequenceStep.Create(seq.Id, 0, SequenceStepType.Email, 0);
        var b = SequenceStep.Create(seq.Id, 1, SequenceStepType.Email, 1);
        seq.AddStep(a);
        seq.AddStep(b);
        var act = () => seq.ReorderSteps(new List<Guid> { a.Id });
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ReorderSteps_WhenValid_ReassignsOrder()
    {
        var seq = NewFocused();
        var a = SequenceStep.Create(seq.Id, 0, SequenceStepType.Email, 0);
        var b = SequenceStep.Create(seq.Id, 1, SequenceStepType.Email, 1);
        seq.AddStep(a);
        seq.AddStep(b);
        seq.ReorderSteps(new List<Guid> { b.Id, a.Id });
        seq.SequenceSteps.Single(s => s.Id == b.Id).OrderIndex.Should().Be(0);
        seq.SequenceSteps.Single(s => s.Id == a.Id).OrderIndex.Should().Be(1);
    }

    [Fact]
    public void ComputeNextStepOrderIndex_UsesMaxPlusOne()
    {
        var seq = NewFocused();
        seq.AddStep(SequenceStep.Create(seq.Id, 0, SequenceStepType.Email, 0));
        seq.AddStep(SequenceStep.Create(seq.Id, 5, SequenceStepType.Email, 0));
        seq.ComputeNextStepOrderIndex().Should().Be(6);
    }

    [Fact]
    public void EnrollProspect_FocusedSecondProspect_Throws()
    {
        var seq = NewFocused();
        seq.EnrollProspect(Guid.NewGuid(), Guid.NewGuid());
        var act = () => seq.EnrollProspect(Guid.NewGuid(), Guid.NewGuid());
        act.Should().Throw<InvalidOperationException>().WithMessage("*only contain one prospect*");
    }

    [Fact]
    public void EnrollProspect_DuplicateProspectId_Throws()
    {
        var seq = NewMulti();
        var pid = Guid.NewGuid();
        seq.EnrollProspect(pid, Guid.NewGuid());
        var act = () => seq.EnrollProspect(pid, Guid.NewGuid());
        act.Should().Throw<InvalidOperationException>().WithMessage("*already enrolled*");
    }

    [Fact]
    public void GetBaselineProspectId_MultiWithNone_Throws()
    {
        var seq = NewMulti();
        var act = () => seq.GetBaselineProspectIdForContentGeneration();
        act.Should().Throw<InvalidOperationException>().WithMessage("*at least one prospect*");
    }

    [Fact]
    public void GetBaselineProspectId_FocusedWithOne_ReturnsId()
    {
        var seq = NewFocused();
        var pid = Guid.NewGuid();
        seq.EnrollProspect(pid, Guid.NewGuid());
        seq.GetBaselineProspectIdForContentGeneration().Should().Be(pid);
    }

    [Fact]
    public void GetStepAtExecutionIndex_RespectsOrderIndex()
    {
        var seq = NewFocused();
        var second = SequenceStep.Create(seq.Id, 1, SequenceStepType.Email, 0);
        var first = SequenceStep.Create(seq.Id, 0, SequenceStepType.LinkedInMessage, 0);
        seq.AddStep(second);
        seq.AddStep(first);
        seq.GetStepAtExecutionIndex(0)!.StepType.Should().Be(SequenceStepType.LinkedInMessage);
        seq.GetStepAtExecutionIndex(1)!.StepType.Should().Be(SequenceStepType.Email);
        Assert.Null(seq.GetStepAtExecutionIndex(2));
    }

    [Fact]
    public void RecordSuccessfulStepAndScheduleNext_LastStep_CompletesProspect()
    {
        var seq = NewFocused();
        seq.AddStep(SequenceStep.Create(seq.Id, 0, SequenceStepType.Email, 0));
        seq.EnrollProspect(Guid.NewGuid(), Guid.NewGuid());
        var sp = seq.SequenceProspects[0];
        sp.Activate(DateTime.UtcNow);
        sp.RecordSuccessfulStepAndScheduleNext(seq, DateTime.UtcNow);
        sp.Status.Should().Be(SequenceProspectStatus.Completed);
    }

    [Fact]
    public void TryCompleteIfNoCurrentStep_WhenIndexPastSteps_Completes()
    {
        var seq = NewFocused();
        seq.AddStep(SequenceStep.Create(seq.Id, 0, SequenceStepType.Email, 0));
        seq.EnrollProspect(Guid.NewGuid(), Guid.NewGuid());
        var sp = seq.SequenceProspects[0];
        sp.Activate(DateTime.UtcNow);
        sp.MarkStepCompleted(DateTime.UtcNow);
        sp.TryCompleteIfNoCurrentStep(seq).Should().BeTrue();
        sp.Status.Should().Be(SequenceProspectStatus.Completed);
    }

    [Fact]
    public void GetMutableStep_UnknownId_ThrowsKeyNotFoundException()
    {
        var seq = NewFocused();
        seq.AddStep(SequenceStep.Create(seq.Id, 0, SequenceStepType.Email, 0));
        var act = () => seq.GetMutableStep(Guid.NewGuid());
        act.Should().Throw<KeyNotFoundException>().WithMessage("*Step not found*");
    }

    [Fact]
    public void GetOutreachChannel_Email_ReturnsEmailChannel()
    {
        var seq = NewFocused();
        var step = SequenceStep.Create(seq.Id, 0, SequenceStepType.Email, 0);
        step.GetOutreachChannel().Should().Be(OutreachChannel.Email);
    }

    [Fact]
    public void TryCompleteIfNoCurrentStep_WhenStepExists_ReturnsFalse()
    {
        var seq = NewFocused();
        seq.AddStep(SequenceStep.Create(seq.Id, 0, SequenceStepType.Email, 0));
        seq.EnrollProspect(Guid.NewGuid(), Guid.NewGuid());
        var sp = seq.SequenceProspects[0];
        sp.Activate(DateTime.UtcNow);
        sp.TryCompleteIfNoCurrentStep(seq).Should().BeFalse();
        sp.Status.Should().Be(SequenceProspectStatus.Active);
    }
}
