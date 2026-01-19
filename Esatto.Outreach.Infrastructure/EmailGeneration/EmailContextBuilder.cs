using Esatto.Outreach.Application.Abstractions;
using Esatto.Outreach.Application.DTOs;
using Esatto.Outreach.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace Esatto.Outreach.Infrastructure.EmailGeneration;

/// <summary>
/// Implementation of email context builder.
/// Orchestrates data fetching from repositories and file system.
/// Follows Clean Architecture: This is where data orchestration happens.
/// </summary>
public sealed class EmailContextBuilder : IEmailContextBuilder
{
    private readonly IProspectRepository _prospectRepo;
    private readonly IEntityIntelligenceRepository _enrichmentRepo;
    private readonly IGenerateEmailPromptRepository _promptRepo;
    private readonly UserManager<ApplicationUser> _userManager;
    private static string? _esattoCompanyInfo;
    private static readonly object _lock = new();

    public EmailContextBuilder(
        IProspectRepository prospectRepo,
        IEntityIntelligenceRepository enrichmentRepo,
        IGenerateEmailPromptRepository promptRepo,
        UserManager<ApplicationUser> userManager)
    {
        _prospectRepo = prospectRepo;
        _enrichmentRepo = enrichmentRepo;
        _promptRepo = promptRepo;
        _userManager = userManager;
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
        EntityIntelligence? entityIntelligence = null;
        if (includeSoftData)
        {
            if (!prospect.EntityIntelligenceId.HasValue)
                throw new InvalidOperationException("No Entity Intelligence available for this prospect. Generate it first.");

            entityIntelligence = await _enrichmentRepo.GetByIdAsync(prospect.EntityIntelligenceId.Value, cancellationToken);
            if (entityIntelligence == null)
                throw new InvalidOperationException("Entity Intelligence record not found.");
        }

        // 4. Hämta aktiv kontaktperson
        ContactPersonContext? activeContactContext = null;
        var activeContact = prospect.GetActiveContact();
        if (activeContact != null)
        {
            activeContactContext = new ContactPersonContext(
                Name: activeContact.Name,
                Title: activeContact.Title,
                Email: activeContact.Email,
                PersonalHooks: activeContact.PersonalHooks?.Count > 0 ? activeContact.PersonalHooks : null,
                PersonalNews: activeContact.PersonalNews?.Count > 0 ? activeContact.PersonalNews : null,
                Summary: activeContact.Summary
            );
        }

        // 5. Bygg request DTO från prospect
        var request = new CustomEmailRequestDto(
            ProspectId: prospect.Id,
            Name: prospect.Name,
            About: prospect.About,
            PictureURL: prospect.PictureURL,
            Websites: prospect.Websites?.Select(w => w.Url).ToList(),
            Addresses: prospect.Addresses?.Select(a => 
                string.Join(", ", new[] { a.Street, a.City, a.State, a.Zip, a.Country }.Where(s => !string.IsNullOrWhiteSpace(s)))
            ).ToList(),
            Tags: prospect.Tags?.Select(t => t.Name).ToList(),
            Notes: prospect.Notes
        );

        // 6. Hämta användarens namn för signatur
        var user = await _userManager.FindByIdAsync(userId);
        var userFullName = user?.FullName ?? user?.UserName ?? "Esatto AB";

        // 7. Skapa och returnera context med aktiv kontakt och användarnamn
        return EmailGenerationContext.Create(
            companyInfo: _esattoCompanyInfo ?? "{}",
            instructions: activePrompt.Instructions,
            request: request,
            entityIntelligence: entityIntelligence,
            activeContact: activeContactContext,
            userFullName: userFullName
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
