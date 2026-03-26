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

public sealed class UpdateProjectCase
{
    private readonly IProjectCaseRepository _caseRepo;
    private readonly ICompanyInfoRepository _companyRepo;

    public UpdateProjectCase(IProjectCaseRepository caseRepo, ICompanyInfoRepository companyRepo)
    {
        _caseRepo = caseRepo;
        _companyRepo = companyRepo;
    }

    public async Task<ProjectCaseDto?> Handle(Guid id, ProjectCaseUpdateDto dto, string userId, CancellationToken ct = default)
    {
        var companyId = await _companyRepo.GetCompanyIdByUserIdAsync(userId, ct);
        if (companyId == null)
            throw new UnauthorizedAccessException("User does not have a company.");

        var pc = await _caseRepo.GetByIdAsync(id, companyId.Value, ct);

        if (pc == null)
            return null;

        pc.ClientName = dto.ClientName;
        pc.Text = dto.Text;
        pc.IsActive = dto.IsActive;
        pc.Touch();

        await _caseRepo.UpdateAsync(pc, ct);

        return new ProjectCaseDto(pc.Id, pc.ClientName, pc.Text, pc.IsActive);
    }
}
