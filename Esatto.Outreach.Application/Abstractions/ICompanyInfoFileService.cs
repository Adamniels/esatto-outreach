using Esatto.Outreach.Application.DTOs;

namespace Esatto.Outreach.Application.Abstractions;

/// <summary>
/// Service for reading company information from JSON file
/// </summary>
public interface ICompanyInfoFileService
{
    /// <summary>
    /// Reads and deserializes company info from esatto-company-info.json
    /// </summary>
    Task<CompanyInfoDto> GetCompanyInfoAsync(CancellationToken ct = default);
}
