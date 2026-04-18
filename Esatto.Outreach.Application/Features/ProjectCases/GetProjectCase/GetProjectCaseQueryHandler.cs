using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Application.Features.Intelligence.Shared;

namespace Esatto.Outreach.Application.Features.ProjectCases.GetProjectCase;

public sealed class GetProjectCaseQueryHandler
{
    private readonly IProjectCaseRepository _caseRepo;
    private readonly ICompanyInfoRepository _companyRepo;

    public GetProjectCaseQueryHandler(IProjectCaseRepository caseRepo, ICompanyInfoRepository companyRepo)
    {
        _caseRepo = caseRepo;
        _companyRepo = companyRepo;
    }

    public async Task<ProjectCaseDto?> Handle(Guid id, string userId, CancellationToken ct = default)
    {
        var companyId = await _companyRepo.GetCompanyIdByUserIdAsync(userId, ct);
        if (companyId == null)
            return null;

        var pc = await _caseRepo.GetByIdAsync(id, companyId.Value, ct);
        if (pc == null)
            return null;

        return new ProjectCaseDto(pc.Id, pc.ClientName, pc.Text, pc.IsActive);
    }
}
