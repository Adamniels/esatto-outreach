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

namespace Esatto.Outreach.Application.UseCases.Intelligence;

public sealed class GetCompanyInfo
{
    private readonly ICompanyInfoRepository _repo;

    public GetCompanyInfo(ICompanyInfoRepository repo)
    {
        _repo = repo;
    }

    public async Task<CompanyInfoDto?> Handle(string userId, CancellationToken ct = default)
    {
        var companyId = await _repo.GetCompanyIdByUserIdAsync(userId, ct);
        if (companyId == null)
            return null;

        var info = await _repo.GetByCompanyIdAsync(companyId.Value, ct);
        if (info == null)
            return null;

        return new CompanyInfoDto(info.Id, info.Company.Name, info.Overview, info.ValueProposition);
    }
}
