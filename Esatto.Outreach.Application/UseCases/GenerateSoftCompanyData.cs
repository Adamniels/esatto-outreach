
using Esatto.Outreach.Application.Abstractions;
using Esatto.Outreach.Application.DTOs;
using Esatto.Outreach.Domain.Entities;
using Esatto.Outreach.Domain.Enums;
using Microsoft.Extensions.Options;

namespace Esatto.Outreach.Application.UseCases;
using Microsoft.Extensions.Logging;

public sealed class GenerateSoftCompanyData
{
    private readonly ISoftCompanyDataRepository _dataRepo;
    private readonly IProspectRepository _prospectRepo;
    private readonly IOpenAIWebSearchClient _researchAgent;
    private readonly ILogger<GenerateSoftCompanyData> _logger;

    public GenerateSoftCompanyData(
        ISoftCompanyDataRepository dataRepo,
        IProspectRepository prospectRepo,
        IOpenAIWebSearchClient chat,
        ILogger<GenerateSoftCompanyData> logger)
    {
        _dataRepo = dataRepo;
        _prospectRepo = prospectRepo;
        _researchAgent = chat;
        _logger = logger;
    }

    public async Task<SoftCompanyDataDto> Handle(
        Guid prospectId,
        CancellationToken ct = default)
    {
        // 1. Hämta prospect från databas
        var prospect = await _prospectRepo.GetByIdAsync(prospectId, ct)
            ?? throw new KeyNotFoundException($"Prospect with ID {prospectId} not found.");

        _logger.LogInformation("Generating soft company data for prospect {ProspectId} ({CompanyName})",
            prospectId, prospect.CompanyName);

        // 2. Anropa OpenAI för att generera research
        var researchResult = await _researchAgent.GenerateCompanyResearchAsync(
            prospect.CompanyName,
            prospect.Domain,
            ct);

        // 3. Skapa eller uppdatera SoftCompanyData entity
        SoftCompanyData softDataEntity;

        if (prospect.SoftCompanyDataId.HasValue)
        {
            // Uppdatera befintlig data
            var existingData = await _dataRepo.GetByIdAsync(prospect.SoftCompanyDataId.Value, ct);
            if (existingData != null)
            {
                existingData.UpdateResearch(
                    hooksJson: researchResult.HooksJson,
                    recentEventsJson: researchResult.RecentEventsJson,
                    newsItemsJson: researchResult.NewsItemsJson,
                    socialActivityJson: researchResult.SocialActivityJson,
                    sourcesJson: researchResult.SourcesJson);

                await _dataRepo.UpdateAsync(existingData, ct);
                softDataEntity = existingData;

                _logger.LogInformation("Updated existing soft data {SoftDataId} for prospect {ProspectId}",
                    existingData.Id, prospectId);
            }
            else
            {
                // Gammal referens finns men entity saknas, skapa ny
                softDataEntity = SoftCompanyData.Create(
                    prospectId: prospectId,
                    hooksJson: researchResult.HooksJson,
                    recentEventsJson: researchResult.RecentEventsJson,
                    newsItemsJson: researchResult.NewsItemsJson,
                    socialActivityJson: researchResult.SocialActivityJson,
                    sourcesJson: researchResult.SourcesJson);

                await _dataRepo.AddAsync(softDataEntity, ct);
                if (prospect.Status == ProspectStatus.New)
                {
                    prospect.SetStatus(ProspectStatus.Researched);
                }
                prospect.LinkSoftCompanyData(softDataEntity.Id);
                await _prospectRepo.UpdateAsync(prospect, ct);
            }
        }
        else
        {
            // Skapa ny SoftCompanyData
            softDataEntity = SoftCompanyData.Create(
                prospectId: prospectId,
                hooksJson: researchResult.HooksJson,
                recentEventsJson: researchResult.RecentEventsJson,
                newsItemsJson: researchResult.NewsItemsJson,
                socialActivityJson: researchResult.SocialActivityJson,
                sourcesJson: researchResult.SourcesJson);

            await _dataRepo.AddAsync(softDataEntity, ct);
            if (prospect.Status == ProspectStatus.New)
            {
                prospect.SetStatus(ProspectStatus.Researched);
            }
            prospect.LinkSoftCompanyData(softDataEntity.Id);
            await _prospectRepo.UpdateAsync(prospect, ct);

            _logger.LogInformation("Created new soft data {SoftDataId} for prospect {ProspectId}",
                softDataEntity.Id, prospectId);
        }

        // 5. Returnera DTO
        return SoftCompanyDataDto.FromEntity(softDataEntity);
    }
}
