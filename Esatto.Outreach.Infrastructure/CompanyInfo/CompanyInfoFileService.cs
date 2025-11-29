using System.Text.Json;
using Esatto.Outreach.Application.Abstractions;
using Esatto.Outreach.Application.DTOs;

namespace Esatto.Outreach.Infrastructure.CompanyInfo;

/// <summary>
/// Service for reading company information from JSON file.
/// Follows the same pattern as EmailContextBuilder but returns structured DTOs.
/// </summary>
public sealed class CompanyInfoFileService : ICompanyInfoFileService
{
    private static readonly object _lock = new();
    private static CompanyInfoDto? _cachedCompanyInfo;
    
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip
    };

    public Task<CompanyInfoDto> GetCompanyInfoAsync(CancellationToken ct = default)
    {
        if (_cachedCompanyInfo != null)
        {
            return Task.FromResult(_cachedCompanyInfo);
        }

        lock (_lock)
        {
            if (_cachedCompanyInfo != null)
            {
                return Task.FromResult(_cachedCompanyInfo);
            }

            try
            {
                var filePath = Path.Combine(AppContext.BaseDirectory, "Data", "esatto-company-info.json");
                
                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException($"Company info file not found at: {filePath}");
                }

                var jsonContent = File.ReadAllText(filePath);
                var jsonArray = JsonSerializer.Deserialize<List<JsonCompanyInfoRoot>>(jsonContent, JsonOptions);

                if (jsonArray == null || jsonArray.Count == 0)
                {
                    throw new InvalidOperationException("Company info JSON is empty or invalid");
                }

                var root = jsonArray[0];
                
                _cachedCompanyInfo = new CompanyInfoDto(
                    Overview: root.Overview ?? string.Empty,
                    Cases: root.Cases?.Select(MapToDto).ToList() ?? new List<CaseItemDto>()
                );

                return Task.FromResult(_cachedCompanyInfo);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to load company info from JSON file", ex);
            }
        }
    }

    private static CaseItemDto MapToDto(JsonCaseItem jsonCase)
    {
        return new CaseItemDto(
            PageTitle: jsonCase.PageTitle ?? string.Empty,
            PageType: jsonCase.PageType ?? string.Empty,
            Case: new CaseDetailDto(
                Name: jsonCase.Case?.Name ?? string.Empty,
                Industry: jsonCase.Case?.Industry ?? string.Empty,
                Challenge: jsonCase.Case?.Challenge ?? string.Empty,
                Solution: jsonCase.Case?.Solution ?? string.Empty,
                Result: jsonCase.Case?.Result ?? string.Empty
            ),
            Services: jsonCase.Services ?? new List<string>(),
            Industries: jsonCase.Industries ?? new List<string>(),
            MethodsOrTech: jsonCase.MethodsOrTech ?? new List<string>(),
            ValuesOrTone: jsonCase.ValuesOrTone ?? new List<string>()
        );
    }

    // Internal JSON deserialization models (matches JSON structure exactly)
    private sealed record JsonCompanyInfoRoot(
        string? Overview,
        List<JsonCaseItem>? Cases
    );

    private sealed record JsonCaseItem(
        string? PageTitle,
        string? PageType,
        JsonCaseDetail? Case,
        List<string>? Services,
        List<string>? Industries,
        List<string>? MethodsOrTech,
        List<string>? ValuesOrTone
    );

    private sealed record JsonCaseDetail(
        string? Name,
        string? Industry,
        string? Challenge,
        string? Solution,
        string? Result
    );
}
