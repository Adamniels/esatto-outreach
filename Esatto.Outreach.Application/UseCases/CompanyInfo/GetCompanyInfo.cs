using Esatto.Outreach.Application.Abstractions;
using Esatto.Outreach.Application.DTOs;

namespace Esatto.Outreach.Application.UseCases.CompanyInfo;

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
