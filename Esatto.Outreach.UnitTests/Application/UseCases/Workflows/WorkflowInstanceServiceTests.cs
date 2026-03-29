using FluentAssertions;
using NSubstitute;
using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Application.Abstractions.Services;
using Esatto.Outreach.Application.Services;
using Esatto.Outreach.Application.UseCases.Workflows;
using Esatto.Outreach.Domain.Entities;
using Esatto.Outreach.Domain.Enums;
using Esatto.Outreach.UnitTests.Helpers;

namespace Esatto.Outreach.UnitTests.Application.UseCases.Workflows;

public class CreateWorkflowInstanceTests
{
    private readonly IWorkflowRepository _workflowRepoMock;
    private readonly IProspectRepository _prospectRepoMock;
    private readonly IOutreachContextBuilder _contextBuilderMock;
    private readonly IOutreachGeneratorFactory _factoryMock;
    private readonly WorkflowDraftGenerator _draftGenerator;
    private readonly CreateWorkflowInstance _sut;

    public CreateWorkflowInstanceTests()
    {
        _workflowRepoMock = Substitute.For<IWorkflowRepository>();
        _prospectRepoMock = Substitute.For<IProspectRepository>();
        _contextBuilderMock = Substitute.For<IOutreachContextBuilder>();
        _factoryMock = Substitute.For<IOutreachGeneratorFactory>();
        
        _draftGenerator = new WorkflowDraftGenerator(_contextBuilderMock, _factoryMock);
        
        _sut = new CreateWorkflowInstance(_workflowRepoMock, _prospectRepoMock, _draftGenerator);
    }

    [Fact]
    public async Task Handle_WithExistingWorkflow_ThrowsInvalidOperationException()
    {
        // Arrange
        var prospectId = Guid.NewGuid();
        var templateId = Guid.NewGuid();
        var existingInstance = TestFactory.CreateWorkflowInstance(prospectId);
        
        _workflowRepoMock.GetInstancesByProspectIdAsync(prospectId, Arg.Any<CancellationToken>())
            .Returns(new List<WorkflowInstance> { existingInstance });

        // Act & Assert
        Func<Task> act = async () => await _sut.Handle(prospectId, templateId, "user-1", CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Prospect already has a workflow*");
    }
}

public class ActivateWorkflowInstanceTests
{
    private readonly IWorkflowRepository _workflowRepoMock;
    private readonly ActivateWorkflowInstance _sut;

    public ActivateWorkflowInstanceTests()
    {
        _workflowRepoMock = Substitute.For<IWorkflowRepository>();
        _sut = new ActivateWorkflowInstance(_workflowRepoMock);
    }

    [Fact]
    public async Task Handle_WithoutActiveContactPerson_ThrowsInvalidOperationException()
    {
        // Arrange
        var prospect = TestFactory.CreateValidManualProspect();
        var id = Guid.NewGuid();
        TestFactory.SetId(prospect, id);
        
        // Prospect has no active contact
        var instance = TestFactory.CreateWorkflowInstance(id);
        
        var prospectProp = typeof(WorkflowInstance).GetProperty("Prospect");
        if(prospectProp != null) prospectProp.SetValue(instance, prospect);

        _workflowRepoMock.GetInstanceByIdAsync(instance.Id, Arg.Any<CancellationToken>())
            .Returns(instance);

        // Act & Assert
        Func<Task> act = async () => await _sut.Handle(instance.Id, "UTC", CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Prospect has no active contact person*");
    }
}
