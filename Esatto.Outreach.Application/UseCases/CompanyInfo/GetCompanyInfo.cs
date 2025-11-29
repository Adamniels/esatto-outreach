using Esatto.Outreach.Application.Abstractions;
using Esatto.Outreach.Application.DTOs;

namespace Esatto.Outreach.Application.UseCases.CompanyInfo;

/// <summary>
/// Use case for retrieving company information from JSON file.
/// Read-only operation - editing happens manually in JSON file.
/// </summary>
public sealed class GetCompanyInfo
{
    private readonly ICompanyInfoFileService _fileService;

    public GetCompanyInfo(ICompanyInfoFileService fileService)
    {
        _fileService = fileService;
    }

    public async Task<CompanyInfoDto> Handle(CancellationToken ct = default)
    {
        return await _fileService.GetCompanyInfoAsync(ct);
    }
}
