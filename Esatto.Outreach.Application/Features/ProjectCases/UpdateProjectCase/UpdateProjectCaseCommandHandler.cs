using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Application.Features.Intelligence.Shared;

namespace Esatto.Outreach.Application.Features.ProjectCases.UpdateProjectCase;

public sealed class UpdateProjectCaseCommandHandler
{
    private readonly IProjectCaseRepository _caseRepo;
    private readonly ICompanyInfoRepository _companyRepo;

    public UpdateProjectCaseCommandHandler(IProjectCaseRepository caseRepo, ICompanyInfoRepository companyRepo)
    {
        _caseRepo = caseRepo;
        _companyRepo = companyRepo;
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
        return new ProjectCaseDto(pc.Id, pc.ClientName, pc.Text, pc.IsActive);
    }
}
