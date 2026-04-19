using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Application.Abstractions.Services;
using Esatto.Outreach.Application.Features.Intelligence.Shared;
using Esatto.Outreach.Application.Features.OutreachGeneration.Shared;
using Esatto.Outreach.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Esatto.Outreach.Domain.Enums;

namespace Esatto.Outreach.Infrastructure.Services.OutreachGeneration;

public sealed class ColdOutreachContextBuilder : IColdOutreachContextBuilder
{
    private readonly IProspectRepository _prospectRepo;
    private readonly IEntityIntelligenceRepository _enrichmentRepo;
    private readonly IOutreachPromptRepository _promptRepo;
    private readonly ICompanyInfoRepository _companyInfoRepo;
    private readonly IProjectCaseRepository _projectCaseRepo;
    private readonly UserManager<ApplicationUser> _userManager;

    public ColdOutreachContextBuilder(
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

    public async Task<ColdOutreachContext> BuildAsync(
        Guid prospectId,
        string userId,
        OutreachChannel channel,
        OutreachGenerationType strategy,
        CancellationToken ct = default)
    {
        var fetchIntelligence = strategy == OutreachGenerationType.UseCollectedData;
        var prospect = await _prospectRepo.GetByIdAsync(prospectId, ct)
            ?? throw new InvalidOperationException($"Prospect {prospectId} not found");

        var generalPrompt = await _promptRepo.GetActiveByUserIdAndTypeAsync(userId, PromptType.General, ct)
            ?? throw new InvalidOperationException("No active general prompt found for this user");

        var targetType = channel == OutreachChannel.Email ? PromptType.Email : PromptType.LinkedIn;
        var specificPrompt = await _promptRepo.GetActiveByUserIdAndTypeAsync(userId, targetType, ct)
            ?? throw new InvalidOperationException($"No active {channel} prompt found for this user");

        var combinedInstructions = $"{generalPrompt.Instructions}\n\n{specificPrompt.Instructions}";

        EntityIntelligence? entityIntelligence = null;
        if (fetchIntelligence)
        {
            if (!prospect.EntityIntelligenceId.HasValue)
                throw new InvalidOperationException("No Entity Intelligence available. Generate it first.");
            entityIntelligence = await _enrichmentRepo.GetByIdAsync(prospect.EntityIntelligenceId.Value, ct)
                ?? throw new InvalidOperationException("Entity Intelligence record not found.");
        }

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
                Summary: activeContact.Summary);
        }

        var prospectInfo = new ProspectInfo(
            ProspectId: prospect.Id,
            Name: prospect.Name,
            About: prospect.About,
            PictureURL: prospect.PictureURL,
            Websites: prospect.Websites?.Select(w => w.Url).ToList(),
            Tags: prospect.Tags?.Select(t => t.Name).ToList(),
            Notes: prospect.Notes);

        var user = await _userManager.FindByIdAsync(userId);
        var userFullName = user?.FullName ?? user?.UserName ?? "Unknown User";

        var companyId = await _companyInfoRepo.GetCompanyIdByUserIdAsync(userId, ct)
            ?? throw new InvalidOperationException("No company associated with this user.");
        var companyInfoEntity = await _companyInfoRepo.GetByCompanyIdAsync(companyId, ct)
            ?? throw new InvalidOperationException("Company information not found.");

        var companyInfo = new CompanyInfoDto(
            companyInfoEntity.Id,
            companyInfoEntity.Company.Name,
            companyInfoEntity.Overview,
            companyInfoEntity.ValueProposition);

        var projectCaseEntities = await _projectCaseRepo.ListByCompanyIdAsync(companyId, ct);
        var projectCases = projectCaseEntities
            .Select(pc => new ProjectCaseDto(pc.Id, pc.ClientName, pc.Text, pc.IsActive))
            .ToList();

        return new ColdOutreachContext
        {
            Prospect = prospectInfo,
            CompanyInfo = companyInfo,
            Instructions = combinedInstructions,
            Channel = channel,
            Strategy = strategy,
            ProjectCases = projectCases,
            EntityIntelligence = entityIntelligence,
            ActiveContact = activeContactContext,
            UserFullName = userFullName
        };
    }
}
