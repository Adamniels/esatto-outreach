using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Application.Abstractions.Services;
using Esatto.Outreach.Application.Features.Intelligence;
using Esatto.Outreach.Application.Features.OutreachGeneration;
using Esatto.Outreach.Domain.Entities;
using Microsoft.AspNetCore.Identity;

using Esatto.Outreach.Domain.Enums;

namespace Esatto.Outreach.Infrastructure.Services.OutreachGeneration;


/// <summary>
/// Implementation of outreach context builder.
/// Orchestrates data fetching from repositories and file system.
/// Follows Clean Architecture: This is where data orchestration happens.
/// </summary>
public sealed class OutreachContextBuilder : IOutreachContextBuilder
{
    private readonly IProspectRepository _prospectRepo;
    private readonly IEntityIntelligenceRepository _enrichmentRepo;
    private readonly IOutreachPromptRepository _promptRepo;
    private readonly ICompanyInfoRepository _companyInfoRepo;
    private readonly IProjectCaseRepository _projectCaseRepo;
    private readonly UserManager<ApplicationUser> _userManager;

    public OutreachContextBuilder(
        IProspectRepository prospectRepo,
        IEntityIntelligenceRepository enrichmentRepo,
        IOutreachPromptRepository promptRepo,
        UserManager<ApplicationUser> userManager,
        ICompanyInfoRepository companyInfoRepo,
        IProjectCaseRepository projectCaseRepo)
    {
        _prospectRepo = prospectRepo;
        _enrichmentRepo = enrichmentRepo;
        _promptRepo = promptRepo;
        _userManager = userManager;
        _companyInfoRepo = companyInfoRepo;
        _projectCaseRepo = projectCaseRepo;
    }

    public async Task<OutreachGenerationContext> BuildContextAsync(
        Guid prospectId,
        string userId,
        OutreachChannel channel,
        bool includeSoftData,
        CancellationToken cancellationToken = default)
    {
        // 1. Hämta prospect
        var prospect = await _prospectRepo.GetByIdAsync(prospectId, cancellationToken);
        if (prospect == null)
            throw new InvalidOperationException($"Prospect with id {prospectId} not found");

        // 2. Fetch General and Channel specific prompts
        var generalPrompt = await _promptRepo.GetActiveByUserIdAndTypeAsync(userId, PromptType.General, cancellationToken);
        var targetType = channel == OutreachChannel.Email ? PromptType.Email : PromptType.LinkedIn;
        var specificPrompt = await _promptRepo.GetActiveByUserIdAndTypeAsync(userId, targetType, cancellationToken);
        
        if (generalPrompt == null)
            throw new InvalidOperationException("No active general prompt template found for this user");
        if (specificPrompt == null)
            throw new InvalidOperationException($"No active {channel} prompt template found for this user");

        var combinedInstructions = $"{generalPrompt.Instructions}\n\n{specificPrompt.Instructions}";

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
        var activeContact = prospect.GetActiveContactQueryHandler();
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
            Tags: prospect.Tags?.Select(t => t.Name).ToList(),
            Notes: prospect.Notes
        );

        // 6. Hämta användarens namn för signatur
        var user = await _userManager.FindByIdAsync(userId);
        var userFullName = user?.FullName ?? user?.UserName ?? "Unknown User";

        // 7. Get company info and project cases
        var companyId = await _companyInfoRepo.GetCompanyIdByUserIdAsync(userId, cancellationToken);
        if (companyId == null)
            throw new InvalidOperationException("No company associated with this user.");

        var companyInfoEntity = await _companyInfoRepo.GetByCompanyIdAsync(companyId.Value, cancellationToken);
        if (companyInfoEntity == null)
            throw new InvalidOperationException("Company information not found.");

        var companyInfo = new CompanyInfoDto(
            companyInfoEntity.Id,
            companyInfoEntity.Company.Name,
            companyInfoEntity.Overview,
            companyInfoEntity.ValueProposition);

        var projectCaseEntities = await _projectCaseRepo.ListByCompanyIdAsync(companyId.Value, cancellationToken);
        var projectCases = projectCaseEntities
            .Select(pc => new ProjectCaseDto(pc.Id, pc.ClientName, pc.Text, pc.IsActive))
            .ToList();

        // 8. Create and return context
        return OutreachGenerationContext.Create(
            companyInfo: companyInfo,
            projectCases: projectCases,
            instructions: combinedInstructions,
            request: request,
            channel: channel,
            entityIntelligence: entityIntelligence,
            activeContact: activeContactContext,
            userFullName: userFullName
        );
    }
}
