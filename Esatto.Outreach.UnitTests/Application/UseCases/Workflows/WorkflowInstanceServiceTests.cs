using FluentAssertions;
using NSubstitute;
using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Application.Abstractions.Services;
using Esatto.Outreach.Application.UseCases.Workflows;
using Esatto.Outreach.Domain.Entities;
using Esatto.Outreach.Domain.Enums;
using Esatto.Outreach.UnitTests.Helpers;

namespace Esatto.Outreach.UnitTests.Application.UseCases.Workflows;

public class WorkflowInstanceServiceTests
{
    private readonly IWorkflowRepository _workflowRepoMock;
    private readonly IProspectRepository _prospectRepoMock;
    private readonly IOutreachContextBuilder _contextBuilderMock;
    private readonly IOutreachGeneratorFactory _factoryMock;
    private readonly GenerateDraftWorkflow _draftService;
    private readonly WorkflowInstanceService _sut;

    public WorkflowInstanceServiceTests()
    {
        _workflowRepoMock = Substitute.For<IWorkflowRepository>();
        _prospectRepoMock = Substitute.For<IProspectRepository>();
        _contextBuilderMock = Substitute.For<IOutreachContextBuilder>();
        _factoryMock = Substitute.For<IOutreachGeneratorFactory>();
        
        // Since GenerateDraftWorkflow is concrete but has injected interfaces, we can build it with mocks
        _draftService = new GenerateDraftWorkflow(_contextBuilderMock, _factoryMock);
        
        _sut = new WorkflowInstanceService(_workflowRepoMock, _prospectRepoMock, _draftService);
    }

    [Fact]
    public async Task CreateInstanceFromTemplateAsync_WithExistingWorkflow_ThrowsInvalidOperationException()
    {
        // Arrange
        var prospectId = Guid.NewGuid();
        var templateId = Guid.NewGuid();
        var existingInstance = TestFactory.CreateWorkflowInstance(prospectId);
        
        _workflowRepoMock.GetInstancesByProspectIdAsync(prospectId, Arg.Any<CancellationToken>())
            .Returns(new List<WorkflowInstance> { existingInstance });

        // Act & Assert
        Func<Task> act = async () => await _sut.CreateInstanceFromTemplateAsync(prospectId, templateId, "user-1", CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Prospect already has a workflow*");
    }

    [Fact]
    public async Task ActivateAsync_WithoutActiveContactPerson_ThrowsInvalidOperationException()
    {
        // Arrange
        var prospect = TestFactory.CreateValidManualProspect();
        var id = Guid.NewGuid();
        TestFactory.SetId(prospect, id);
        
        // Prospect has no active contact (we didn't call AddContactPerson or SetActiveContact)
        var instance = TestFactory.CreateWorkflowInstance(id);
        
        // Use reflection to set the private navigation property for testing since EF would normally map this
        var prospectProp = typeof(WorkflowInstance).GetProperty("Prospect");
        if(prospectProp != null) prospectProp.SetValue(instance, prospect);

        _workflowRepoMock.GetInstanceByIdAsync(instance.Id, Arg.Any<CancellationToken>())
            .Returns(instance);

        // Act & Assert
        Func<Task> act = async () => await _sut.ActivateAsync(instance.Id, "UTC", CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Prospect has no active contact person*");
    }
}
