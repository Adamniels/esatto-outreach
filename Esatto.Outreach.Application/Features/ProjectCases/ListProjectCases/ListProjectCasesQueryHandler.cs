using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Application.Features.Intelligence.Shared;

namespace Esatto.Outreach.Application.Features.ProjectCases.ListProjectCases;

public sealed class ListProjectCasesQueryHandler
{
    private readonly IProjectCaseRepository _caseRepo;
    private readonly ICompanyInfoRepository _companyRepo;

    public ListProjectCasesQueryHandler(IProjectCaseRepository caseRepo, ICompanyInfoRepository companyRepo)
    {
        _caseRepo = caseRepo;
        _companyRepo = companyRepo;
    }

    public async Task<List<ProjectCaseDto>> Handle(ListProjectCasesQuery query, string userId, CancellationToken ct = default)
    {
        var companyId = await _companyRepo.GetCompanyIdByUserIdAsync(userId, ct);
        if (companyId == null)
            return new List<ProjectCaseDto>();

        var cases = await _caseRepo.ListByCompanyIdAsync(companyId.Value, ct);
        return cases.Select(x => new ProjectCaseDto(x.Id, x.ClientName, x.Text, x.IsActive)).ToList();
    }
}
