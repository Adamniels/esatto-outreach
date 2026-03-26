using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Application.Abstractions.Services;
using Esatto.Outreach.Application.Abstractions.Clients;
using Esatto.Outreach.Application.DTOs;
using Esatto.Outreach.Application.DTOs.Prospects;
using Esatto.Outreach.Application.DTOs.Auth;
using Esatto.Outreach.Application.DTOs.Intelligence;
using Esatto.Outreach.Application.DTOs.Outreach;
using Esatto.Outreach.Application.DTOs.Webhooks;
using Esatto.Outreach.Application.DTOs.Workflows;
using Microsoft.Extensions.Logging;

namespace Esatto.Outreach.Application.UseCases.Webhooks;

/// <summary>
/// Claimar en pending prospect - "först till kvarn" systemet.
/// Sätter OwnerId och ändrar IsPending till false.
/// </summary>
public class ClaimPendingProspect
{
    private readonly IProspectRepository _prospectRepo;
    private readonly ILogger<ClaimPendingProspect> _logger;

    public ClaimPendingProspect(
        IProspectRepository prospectRepo,
        ILogger<ClaimPendingProspect> logger)
    {
        _prospectRepo = prospectRepo;
        _logger = logger;
    }

    public async Task<ProspectViewDto?> Handle(
        Guid prospectId,
        string userId,
        CancellationToken ct = default)
    {
        if (prospectId == Guid.Empty)
        {
            throw new InvalidOperationException("Invalid prospect ID");
        }

        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new InvalidOperationException("User ID is required");
        }

        var prospect = await _prospectRepo.GetByIdAsync(prospectId, ct);

        if (prospect == null)
        {
            _logger.LogWarning("Prospect not found: {ProspectId}", prospectId);
            return null;
        }

        if (!prospect.IsPending)
        {
            _logger.LogWarning(
                "Prospect is not pending: {ProspectId}, IsPending: {IsPending}",
                prospectId,
                prospect.IsPending);
            throw new InvalidOperationException("Prospect is not pending");
        }

        if (!prospect.IsFromCrm)
        {
            _logger.LogWarning(
                "Cannot claim non-CRM prospect: {ProspectId}",
                prospectId);
            throw new InvalidOperationException("Can only claim CRM-imported prospects");
        }

        prospect.Claim(userId);
        await _prospectRepo.UpdateAsync(prospect, ct);

        _logger.LogInformation(
            "Prospect claimed by user: {UserId} - '{ProspectName}' (ID: {ProspectId})",
            userId,
            prospect.Name,
            prospectId);

        return ProspectViewDto.FromEntity(prospect);
    }
}
