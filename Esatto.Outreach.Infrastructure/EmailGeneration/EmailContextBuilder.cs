using Esatto.Outreach.Application.Abstractions;
using Esatto.Outreach.Application.DTOs;
using Esatto.Outreach.Domain.Entities;

namespace Esatto.Outreach.Infrastructure.EmailGeneration;

/// <summary>
/// Implementation of email context builder.
/// Orchestrates data fetching from repositories and file system.
/// Follows Clean Architecture: This is where data orchestration happens.
/// </summary>
public sealed class EmailContextBuilder : IEmailContextBuilder
{
    private readonly IProspectRepository _prospectRepo;
    private readonly ISoftCompanyDataRepository _softDataRepo;
    private readonly IGenerateEmailPromptRepository _promptRepo;
    private static string? _esattoCompanyInfo;
    private static readonly object _lock = new();

    public EmailContextBuilder(
        IProspectRepository prospectRepo,
        ISoftCompanyDataRepository softDataRepo,
        IGenerateEmailPromptRepository promptRepo)
    {
        _prospectRepo = prospectRepo;
        _softDataRepo = softDataRepo;
        _promptRepo = promptRepo;
        LoadEsattoCompanyInfo();
    }

    public async Task<EmailGenerationContext> BuildContextAsync(
        Guid prospectId,
        string userId,
        bool includeSoftData,
        CancellationToken cancellationToken = default)
    {
        // 1. Hämta prospect
        var prospect = await _prospectRepo.GetByIdAsync(prospectId, cancellationToken);
        if (prospect == null)
            throw new InvalidOperationException($"Prospect with id {prospectId} not found");

        // 2. Hämta aktiv prompt för användaren
        var activePrompt = await _promptRepo.GetActiveByUserIdAsync(userId, cancellationToken);
        if (activePrompt == null)
            throw new InvalidOperationException("No active email prompt template found for this user");

        // 3. Hämta soft data om det krävs
        SoftCompanyData? softData = null;
        if (includeSoftData)
        {
            if (!prospect.SoftCompanyDataId.HasValue)
                throw new InvalidOperationException("No collected soft data available for this prospect. Generate soft data first.");

            softData = await _softDataRepo.GetByIdAsync(prospect.SoftCompanyDataId.Value, cancellationToken);
            if (softData == null)
                throw new InvalidOperationException("No collected soft data available for this prospect. Generate soft data first.");
        }

        // 4. Bygg request DTO från prospect
        var request = new CustomEmailRequestDto(
            ProspectId: prospect.Id,
            Name: prospect.Name,
            About: prospect.About,
            PictureURL: prospect.PictureURL,
            Websites: prospect.Websites?.Select(w => w.Url).ToList(),
            EmailAddresses: prospect.EmailAddresses?.Select(e => e.Address).ToList(),
            PhoneNumbers: prospect.PhoneNumbers?.Select(p => p.Number).ToList(),
            Addresses: prospect.Addresses?.Select(a => 
                string.Join(", ", new[] { a.Street, a.City, a.State, a.Zip, a.Country }.Where(s => !string.IsNullOrWhiteSpace(s)))
            ).ToList(),
            Tags: prospect.Tags?.Select(t => t.Name).ToList(),
            Notes: prospect.Notes
        );

        // 5. Skapa och returnera context
        return EmailGenerationContext.Create(
            companyInfo: _esattoCompanyInfo ?? "{}",
            instructions: activePrompt.Instructions,
            request: request,
            softData: softData
        );
    }

    private static void LoadEsattoCompanyInfo()
    {
        if (_esattoCompanyInfo != null) return;
        
        lock (_lock)
        {
            if (_esattoCompanyInfo != null) return;

            try
            {
                var filePath = Path.Combine(AppContext.BaseDirectory, "Data", "esatto-company-info.json");
                if (File.Exists(filePath))
                {
                    _esattoCompanyInfo = File.ReadAllText(filePath);
                }
                else
                {
                    _esattoCompanyInfo = "{}";
                }
            }
            catch
            {
                _esattoCompanyInfo = "{}";
            }
        }
    }
}
