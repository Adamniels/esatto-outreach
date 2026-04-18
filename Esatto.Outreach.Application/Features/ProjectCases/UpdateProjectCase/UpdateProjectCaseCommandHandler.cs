using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Application.Features.Intelligence.Shared;

namespace Esatto.Outreach.Application.Features.ProjectCases.UpdateProjectCase;

public sealed class UpdateProjectCaseCommandHandler
{
    private readonly IProjectCaseRepository _caseRepo;
    private readonly ICompanyInfoRepository _companyRepo;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateProjectCaseCommandHandler(IProjectCaseRepository caseRepo, ICompanyInfoRepository companyRepo, IUnitOfWork unitOfWork)
    {
        _caseRepo = caseRepo;
        _companyRepo = companyRepo;
        _unitOfWork = unitOfWork;
    }

    public async Task<ProjectCaseDto?> Handle(UpdateProjectCaseCommand command, string userId, CancellationToken ct = default)
    {
        var companyId = await _companyRepo.GetCompanyIdByUserIdAsync(userId, ct);
        if (companyId == null)
            throw new UnauthorizedAccessException("User does not have a company.");

        var pc = await _caseRepo.GetByIdAsync(command.Id, companyId.Value, ct);
        if (pc == null)
            return null;

        pc.ClientName = command.ClientName;
        pc.Text = command.Text;
        pc.IsActive = command.IsActive;
        pc.Touch();

        await _caseRepo.UpdateAsync(pc, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return new ProjectCaseDto(pc.Id, pc.ClientName, pc.Text, pc.IsActive);
    }
}
