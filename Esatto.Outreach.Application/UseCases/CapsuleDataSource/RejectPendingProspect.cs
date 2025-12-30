using Esatto.Outreach.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace Esatto.Outreach.Application.UseCases.CapsuleDataSource;

/// <summary>
/// Raderar en pending prospect helt från systemet (reject).
/// Kan bara raderas om den är pending.
/// </summary>
public class RejectPendingProspect
{
    private readonly IProspectRepository _prospectRepo;
    private readonly ILogger<RejectPendingProspect> _logger;

    public RejectPendingProspect(
        IProspectRepository prospectRepo,
        ILogger<RejectPendingProspect> logger)
    {
        _prospectRepo = prospectRepo;
        _logger = logger;
    }

    public async Task<bool> Handle(
        Guid prospectId,
        CancellationToken ct = default)
    {
        if (prospectId == Guid.Empty)
        {
            throw new InvalidOperationException("Invalid prospect ID");
        }

        var prospect = await _prospectRepo.GetByIdAsync(prospectId, ct);

        if (prospect == null)
        {
            _logger.LogWarning("Prospect not found: {ProspectId}", prospectId);
            return false;
        }

        if (!prospect.IsPending)
        {
            _logger.LogWarning(
                "Cannot reject prospect that is not pending: {ProspectId}",
                prospectId);
            throw new InvalidOperationException("Can only reject pending prospects");
        }

        var prospectName = prospect.Name;
        await _prospectRepo.DeleteAsync(prospectId, ct);

        _logger.LogInformation(
            "Rejected and deleted pending prospect: '{ProspectName}' (ID: {ProspectId})",
            prospectName,
            prospectId);

        return true;
    }
}

