using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Application.Features.Intelligence;
using Esatto.Outreach.Domain.Entities;

namespace Esatto.Outreach.Application.Features.Intelligence;

public sealed class UpdateCompanyInfoCommandHandler
{
    private readonly ICompanyInfoRepository _repo;
    private readonly ICompanyRepository _companyRepo;

    public UpdateCompanyInfoCommandHandler(ICompanyInfoRepository repo, ICompanyRepository companyRepo)
    {
        _repo = repo;
        _companyRepo = companyRepo;
    }

    public async Task<CompanyInfoDto?> Handle(string userId, CompanyInfoUpdateDto dto, CancellationToken ct = default)
    {
        var companyId = await _repo.GetCompanyIdByUserIdAsync(userId, ct);
        if (companyId == null)
            throw new UnauthorizedAccessException("User does not have a company.");

        var company = await _companyRepo.GetByIdAsync(companyId.Value, ct);
        if (company == null)
            throw new UnauthorizedAccessException("Company not found.");

        // Update the Company name
        company.Name = dto.Name;
        await _companyRepo.UpdateAsync(company, ct);

        var info = await _repo.GetByCompanyIdAsync(companyId.Value, ct);

        if (info == null)
        {
            info = new CompanyInformation
            {
                CompanyId = companyId.Value,
                Overview = dto.Overview,
                ValueProposition = dto.ValueProposition
            };
            await _repo.AddAsync(info, ct);
        }
        else
        {
            info.Overview = dto.Overview;
            info.ValueProposition = dto.ValueProposition;
            info.Touch();
            await _repo.UpdateAsync(info, ct);
        }

        return new CompanyInfoDto(info.Id, company.Name, info.Overview, info.ValueProposition);
    }
}
