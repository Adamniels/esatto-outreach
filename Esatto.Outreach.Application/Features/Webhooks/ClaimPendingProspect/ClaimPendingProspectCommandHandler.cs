using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Application.Features.Prospects.Shared;
using Microsoft.Extensions.Logging;

namespace Esatto.Outreach.Application.Features.Webhooks.ClaimPendingProspect;

public class ClaimPendingProspectCommandHandler
{
    private readonly IProspectRepository _prospectRepo;
    private readonly ILogger<ClaimPendingProspectCommandHandler> _logger;

    public ClaimPendingProspectCommandHandler(IProspectRepository prospectRepo, ILogger<ClaimPendingProspectCommandHandler> logger)
    {
        _prospectRepo = prospectRepo;
        _logger = logger;
    }

    public async Task<ProspectViewDto?> Handle(ClaimPendingProspectCommand command, string userId, CancellationToken ct = default)
    {
        if (command.ProspectId == Guid.Empty) throw new InvalidOperationException("Invalid prospect ID");
        if (string.IsNullOrWhiteSpace(userId)) throw new InvalidOperationException("User ID is required");

        var prospect = await _prospectRepo.GetByIdAsync(command.ProspectId, ct);
        if (prospect == null) return null;
        if (!prospect.IsPending) throw new InvalidOperationException("Prospect is not pending");
        if (!prospect.IsFromCrm) throw new InvalidOperationException("Can only claim CRM-imported prospects");

        prospect.Claim(userId);
        await _prospectRepo.UpdateAsync(prospect, ct);
        _logger.LogInformation("Prospect claimed by user: {UserId} - '{ProspectName}' (ID: {ProspectId})", userId, prospect.Name, command.ProspectId);
        return ProspectViewDto.FromEntity(prospect);
    }
}
