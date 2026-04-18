using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Application.Features.Intelligence.Shared;
using Esatto.Outreach.Domain.Entities;

namespace Esatto.Outreach.Application.Features.Intelligence.UpdateCompanyInfo;

public sealed class UpdateCompanyInfoCommandHandler
{
    private readonly ICompanyInfoRepository _repo;
    private readonly ICompanyRepository _companyRepo;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateCompanyInfoCommandHandler(ICompanyInfoRepository repo, ICompanyRepository companyRepo, IUnitOfWork unitOfWork)
    {
        _repo = repo;
        _companyRepo = companyRepo;
        _unitOfWork = unitOfWork;
    }

    public async Task<CompanyInfoDto?> Handle(UpdateCompanyInfoCommand command, string userId, CancellationToken ct = default)
    {
        var companyId = await _repo.GetCompanyIdByUserIdAsync(userId, ct);
        if (companyId == null)
            throw new UnauthorizedAccessException("User does not have a company.");

        var company = await _companyRepo.GetByIdAsync(companyId.Value, ct);
        if (company == null)
            throw new UnauthorizedAccessException("Company not found.");

        company.Name = command.Name;
        await _companyRepo.UpdateAsync(company, ct);

        var info = await _repo.GetByCompanyIdAsync(companyId.Value, ct);

        if (info == null)
        {
            info = new CompanyInformation
            {
                CompanyId = companyId.Value,
                Overview = command.Overview,
                ValueProposition = command.ValueProposition
            };
            await _repo.AddAsync(info, ct);
        }
        else
        {
            info.Overview = command.Overview;
            info.ValueProposition = command.ValueProposition;
            info.Touch();
            await _repo.UpdateAsync(info, ct);
        }

        await _unitOfWork.SaveChangesAsync(ct);
        return new CompanyInfoDto(info.Id, company.Name, info.Overview, info.ValueProposition);
    }
}
