using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Application.Features.Intelligence.Shared;
using Esatto.Outreach.Domain.Entities;

namespace Esatto.Outreach.Application.Features.ProjectCases.CreateProjectCase;

public sealed class CreateProjectCaseCommandHandler
{
    private readonly IProjectCaseRepository _caseRepo;
    private readonly ICompanyInfoRepository _companyRepo;

    public CreateProjectCaseCommandHandler(IProjectCaseRepository caseRepo, ICompanyInfoRepository companyRepo)
    {
        _caseRepo = caseRepo;
        _companyRepo = companyRepo;
    }

    public async Task<ProjectCaseDto> Handle(CreateProjectCaseCommand command, string userId, CancellationToken ct = default)
    {
        var companyId = await _companyRepo.GetCompanyIdByUserIdAsync(userId, ct);
        if (companyId == null)
            throw new UnauthorizedAccessException("User does not have a company.");

        var pc = new ProjectCase
        {
            CompanyId = companyId.Value,
            ClientName = command.ClientName,
            Text = command.Text,
            IsActive = command.IsActive
        };

        await _caseRepo.AddAsync(pc, ct);
        return new ProjectCaseDto(pc.Id, pc.ClientName, pc.Text, pc.IsActive);
    }
}
