using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Application.DTOs.Intelligence;
using Esatto.Outreach.Domain.Entities;

namespace Esatto.Outreach.Application.UseCases.ProjectCases;

public sealed class CreateProjectCase
{
    private readonly IProjectCaseRepository _caseRepo;
    private readonly ICompanyInfoRepository _companyRepo;

    public CreateProjectCase(IProjectCaseRepository caseRepo, ICompanyInfoRepository companyRepo)
    {
        _caseRepo = caseRepo;
        _companyRepo = companyRepo;
    }

    public async Task<ProjectCaseDto> Handle(ProjectCaseUpdateDto dto, string userId, CancellationToken ct = default)
    {
        var companyId = await _companyRepo.GetCompanyIdByUserIdAsync(userId, ct);
        if (companyId == null)
            throw new UnauthorizedAccessException("User does not have a company.");

        var pc = new ProjectCase
        {
            CompanyId = companyId.Value,
            ClientName = dto.ClientName,
            Text = dto.Text,
            IsActive = dto.IsActive
        };

        await _caseRepo.AddAsync(pc, ct);

        return new ProjectCaseDto(pc.Id, pc.ClientName, pc.Text, pc.IsActive);
    }
}
