using FluentAssertions;
using NSubstitute;
using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Application.Abstractions.Services;
using Esatto.Outreach.Application.Features.OutreachGeneration.GenerateEmailDraft;
using Esatto.Outreach.Application.Features.OutreachGeneration.Shared;
using Esatto.Outreach.Application.Features.Intelligence.Shared;
using Esatto.Outreach.Domain.Entities;
using Esatto.Outreach.Domain.Enums;
using Esatto.Outreach.UnitTests.Helpers;

namespace Esatto.Outreach.UnitTests.Application.Features.OutreachGeneration.GenerateEmailDraft;

public class GenerateMailTests
{
    private readonly IColdOutreachContextBuilder _contextBuilderMock;
    private readonly IColdOutreachGeneratorFactory _factoryMock;
    private readonly IColdOutreachGenerator _generatorMock;
    private readonly IProspectRepository _prospectRepoMock;
    private readonly IUnitOfWork _unitOfWorkMock;
    private readonly GenerateMailCommandHandler _sut;

    public GenerateMailTests()
    {
        _contextBuilderMock = Substitute.For<IColdOutreachContextBuilder>();
        _factoryMock = Substitute.For<IColdOutreachGeneratorFactory>();
        _generatorMock = Substitute.For<IColdOutreachGenerator>();
        _prospectRepoMock = Substitute.For<IProspectRepository>();
        _unitOfWorkMock = Substitute.For<IUnitOfWork>();

        _factoryMock.GetGenerator(Arg.Any<OutreachGenerationType?>()).Returns(_generatorMock);

        _sut = new GenerateMailCommandHandler(_contextBuilderMock, _factoryMock, _prospectRepoMock, _unitOfWorkMock);
    }

    [Fact]
    public async Task Handle_WithValidProspect_GeneratesDraftAndSavesToProspect()
    {
        // Arrange
        var prospectId = Guid.NewGuid();
        var userId = "user-1";
        var prospect = TestFactory.CreateValidManualProspect();
        TestFactory.SetId(prospect, prospectId);

        _prospectRepoMock.GetByIdAsync(prospectId, Arg.Any<CancellationToken>())
            .Returns(prospect);

        var dummyContext = new ColdOutreachContext
        {
            CompanyInfo = new CompanyInfoDto(Guid.NewGuid(), "Test", "Test", "Test"),
            Instructions = "test instructions",
            Prospect = new ProspectInfo(prospectId, "Test Name", null, null, null, null, null),
            Channel = OutreachChannel.Email
        };

        _contextBuilderMock.BuildAsync(prospectId, userId, OutreachChannel.Email, Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(dummyContext);

        var draftResult = new CustomOutreachDraftDto(
            Title: "Test Subject",
            BodyPlain: "Test plain body",
            BodyHTML: "<html><p>Test</p></html>"
        );

        _generatorMock.GenerateAsync(dummyContext, Arg.Any<CancellationToken>())
            .Returns(draftResult);

        // Act
        var result = await _sut.Handle(new GenerateMailCommand(prospectId, null), userId, CancellationToken.None);

        // Assert
        ObjectAssertion.Should(result).NotBeNull();
        prospect.MailTitle.Should().Be("Test Subject");
        prospect.MailBodyPlain.Should().Be("Test plain body");

        await _prospectRepoMock.Received(1).UpdateAsync(prospect, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithMissingProspect_ThrowsInvalidOperationException()
    {
        // Arrange
        var prospectId = Guid.NewGuid();

        var dummyContext = new ColdOutreachContext
        {
            CompanyInfo = new CompanyInfoDto(Guid.NewGuid(), "Test", "Test", "Test"),
            Instructions = "test",
            Prospect = new ProspectInfo(prospectId, "Test", null, null, null, null, null),
            Channel = OutreachChannel.Email
        };

        _contextBuilderMock.BuildAsync(prospectId, "u-1", OutreachChannel.Email, Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(dummyContext);

        _generatorMock.GenerateAsync(Arg.Any<ColdOutreachContext>(), Arg.Any<CancellationToken>())
            .Returns(new CustomOutreachDraftDto("A", "B", "C"));

        _prospectRepoMock.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Prospect)null!);

        // Act & Assert
        Func<Task> act = async () => await _sut.Handle(new GenerateMailCommand(prospectId, null), "u-1", CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*");
    }
}
