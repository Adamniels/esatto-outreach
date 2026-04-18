using Esatto.Outreach.Application.Abstractions.Repositories;
using Microsoft.Extensions.Logging;

namespace Esatto.Outreach.Application.Features.Webhooks.RejectPendingProspect;

public class RejectPendingProspectCommandHandler
{
    private readonly IProspectRepository _prospectRepo;
    private readonly ILogger<RejectPendingProspectCommandHandler> _logger;

    public RejectPendingProspectCommandHandler(IProspectRepository prospectRepo, ILogger<RejectPendingProspectCommandHandler> logger)
    {
        _prospectRepo = prospectRepo;
        _logger = logger;
    }

    public async Task<bool> Handle(Guid prospectId, CancellationToken ct = default)
    {
        if (prospectId == Guid.Empty) throw new InvalidOperationException("Invalid prospect ID");
        var prospect = await _prospectRepo.GetByIdAsync(prospectId, ct);
        if (prospect == null) return false;
        if (!prospect.IsPending) throw new InvalidOperationException("Can only reject pending prospects");

        var prospectName = prospect.Name;
        await _prospectRepo.DeleteAsync(prospectId, ct);
        _logger.LogInformation("Rejected and deleted pending prospect: '{ProspectName}' (ID: {ProspectId})", prospectName, prospectId);
        return true;
    }
}
