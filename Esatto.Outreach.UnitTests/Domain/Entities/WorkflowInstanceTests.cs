using FluentAssertions;
using Esatto.Outreach.Domain.Entities;
using Esatto.Outreach.Domain.Enums;
using Esatto.Outreach.UnitTests.Helpers;

namespace Esatto.Outreach.UnitTests.Domain.Entities;

public class WorkflowInstanceTests
{
    [Fact]
    public void Create_ReturnsDraftWorkflowInstance()
    {
        // Arrange
        var prospectId = Guid.NewGuid();

        // Act
        var instance = WorkflowInstance.Create(prospectId);

        // Assert
        instance.Should().NotBeNull();
        instance.ProspectId.Should().Be(prospectId);
        instance.Status.Should().Be(WorkflowStatus.Draft);
        instance.Steps.Should().BeEmpty();
        instance.TimeZoneId.Should().Be("UTC"); // Default Configured
    }

    [Fact]
    public void AddStep_WithValidParameters_AddsStepCorrectly()
    {
        // Arrange
        var instance = TestFactory.CreateWorkflowInstance();

        // Act
        instance.AddStep(WorkflowStepType.Email, 1, new TimeSpan(9, 0, 0), ContentGenerationStrategy.UseCollectedData);

        // Assert
        instance.Steps.Should().HaveCount(1);
        var addedStep = instance.Steps.First();
        addedStep.Type.Should().Be(WorkflowStepType.Email);
        addedStep.DayOffset.Should().Be(1);
        addedStep.TimeOfDay.Should().Be(new TimeSpan(9, 0, 0));
        addedStep.GenerationStrategy.Should().Be(ContentGenerationStrategy.UseCollectedData);
    }

    [Fact]
    public void AddStep_WithMissingGenerationStrategyForEmail_ThrowsArgumentException()
    {
        // Arrange
        var instance = TestFactory.CreateWorkflowInstance();

        // Act & Assert
        Action act = () => instance.AddStep(WorkflowStepType.Email, 1, new TimeSpan(9, 0, 0), null);
        act.Should().Throw<ArgumentException>().WithMessage("*requires a content generation strategy*");
    }

    [Fact]
    public void CanActivate_WithNoMissingDependencies_ReturnsEmptyErrors()
    {
        // Arrange
        var instance = TestFactory.CreateWorkflowInstance();
        instance.AddStep(WorkflowStepType.Email, 1, new TimeSpan(9, 0, 0), ContentGenerationStrategy.UseCollectedData);

        // Act
        // True because prospect HAS entity intelligence
        var errors = instance.CanActivate(hasEntityIntelligence: true);

        // Assert
        errors.Should().BeEmpty();
    }

    [Fact]
    public void CanActivate_WithUseCollectedDataButNoIntelligence_ReturnsDependenciesErrors()
    {
        // Arrange
        var instance = TestFactory.CreateWorkflowInstance();
        instance.AddStep(WorkflowStepType.Email, 1, new TimeSpan(9, 0, 0), ContentGenerationStrategy.UseCollectedData);

        // Act
        // False because prospect DOES NOT HAVE entity intelligence
        var errors = instance.CanActivate(hasEntityIntelligence: false);

        // Assert
        errors.Should().NotBeEmpty();
        errors.First().Should().Contain("requires Entity Intelligence");
    }
    
    [Fact]
    public void CanActivate_WithEmailButNoGenerationStrategy_ReturnsStrategyError()
    {
        // Arrange
        var instance = TestFactory.CreateWorkflowInstance();

        // Need to forcefully inject an invalid step to test CanActivate's error trapping
        // (AddStep enforces generation strategy at creation, but let's assume one got through via bad DB data)
        var invalidStep = WorkflowStep.Create(WorkflowStepType.LinkedInConnectionRequest, 1, TimeSpan.Zero, 0, null);
        
        // Manual setter hack to simulate corrupt data for testing CanActivate
        var typeProp = invalidStep.GetType().GetProperty("Type");
        if (typeProp != null) typeProp.SetValue(invalidStep, WorkflowStepType.Email);
        
        instance.AddStep(invalidStep);

        // Act
        var errors = instance.CanActivate(hasEntityIntelligence: true);

        // Assert
        errors.Should().NotBeEmpty();
        errors.First().Should().Contain("is missing a generation strategy");
    }
}
