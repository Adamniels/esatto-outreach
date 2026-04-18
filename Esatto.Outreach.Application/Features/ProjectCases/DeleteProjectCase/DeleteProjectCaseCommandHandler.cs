using Esatto.Outreach.Application.Abstractions.Repositories;

namespace Esatto.Outreach.Application.Features.ProjectCases.DeleteProjectCase;

public sealed class DeleteProjectCaseCommandHandler
{
    private readonly IProjectCaseRepository _caseRepo;
    private readonly ICompanyInfoRepository _companyRepo;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteProjectCaseCommandHandler(IProjectCaseRepository caseRepo, ICompanyInfoRepository companyRepo, IUnitOfWork unitOfWork)
    {
        _caseRepo = caseRepo;
        _companyRepo = companyRepo;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(DeleteProjectCaseCommand command, string userId, CancellationToken ct = default)
    {
        var companyId = await _companyRepo.GetCompanyIdByUserIdAsync(userId, ct);
        if (companyId == null)
            throw new UnauthorizedAccessException("User does not have a company.");

        var pc = await _caseRepo.GetByIdAsync(command.Id, companyId.Value, ct);
        if (pc == null)
            return false;

        await _caseRepo.DeleteAsync(pc, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return true;
    }
}
