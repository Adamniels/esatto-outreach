using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Application.Abstractions.Services;
using Esatto.Outreach.Application.Abstractions.Clients;
using Esatto.Outreach.Application.DTOs;
using Esatto.Outreach.Application.DTOs.Prospects;
using Esatto.Outreach.Application.DTOs.Auth;
using Esatto.Outreach.Application.DTOs.Intelligence;
using Esatto.Outreach.Application.DTOs.Outreach;
using Esatto.Outreach.Application.DTOs.Webhooks;
using Esatto.Outreach.Application.DTOs.Workflows;

namespace Esatto.Outreach.Application.UseCases.ProjectCases;

public sealed class GetProjectCases
{
    private readonly IProjectCaseRepository _caseRepo;
    private readonly ICompanyInfoRepository _companyRepo;

    public GetProjectCases(IProjectCaseRepository caseRepo, ICompanyInfoRepository companyRepo)
    {
        _caseRepo = caseRepo;
        _companyRepo = companyRepo;
    }

    public async Task<List<ProjectCaseDto>> Handle(string userId, CancellationToken ct = default)
    {
        var companyId = await _companyRepo.GetCompanyIdByUserIdAsync(userId, ct);
        if (companyId == null)
            return new List<ProjectCaseDto>();

        var cases = await _caseRepo.ListByCompanyIdAsync(companyId.Value, ct);

        return cases.Select(x => new ProjectCaseDto(x.Id, x.ClientName, x.Text, x.IsActive)).ToList();
    }
}
