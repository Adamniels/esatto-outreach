using System.Threading;
using System.Threading.Tasks;
using Esatto.Outreach.Application.DTOs;

namespace Esatto.Outreach.Application.Abstractions;

public interface IOpenAIWebSearchClient
{
    Task<SoftCompanyDataDto> GenerateCompanyResearchAsync(
        string companyName,
        string? domain,
        CancellationToken ct = default);
}
