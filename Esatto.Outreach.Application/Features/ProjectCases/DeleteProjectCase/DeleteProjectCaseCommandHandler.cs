using Esatto.Outreach.Application.Abstractions.Repositories;

namespace Esatto.Outreach.Application.Features.ProjectCases.DeleteProjectCase;

public sealed class DeleteProjectCaseCommandHandler
{
    private readonly IProjectCaseRepository _caseRepo;
    private readonly ICompanyInfoRepository _companyRepo;

    public DeleteProjectCaseCommandHandler(IProjectCaseRepository caseRepo, ICompanyInfoRepository companyRepo)
    {
        _caseRepo = caseRepo;
        _companyRepo = companyRepo;
    }

    public async Task<bool> Handle(Guid id, string userId, CancellationToken ct = default)
    {
        var companyId = await _companyRepo.GetCompanyIdByUserIdAsync(userId, ct);
        if (companyId == null)
            throw new UnauthorizedAccessException("User does not have a company.");

        var pc = await _caseRepo.GetByIdAsync(id, companyId.Value, ct);
        if (pc == null)
            return false;

        await _caseRepo.DeleteAsync(pc, ct);
        return true;
    }
}
