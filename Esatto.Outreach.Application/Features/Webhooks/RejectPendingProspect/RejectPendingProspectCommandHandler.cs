using Esatto.Outreach.Application.Abstractions.Repositories;
using Microsoft.Extensions.Logging;

namespace Esatto.Outreach.Application.Features.Webhooks.RejectPendingProspect;

public class RejectPendingProspectCommandHandler
{
    private readonly IProspectRepository _prospectRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RejectPendingProspectCommandHandler> _logger;

    public RejectPendingProspectCommandHandler(IProspectRepository prospectRepo, IUnitOfWork unitOfWork, ILogger<RejectPendingProspectCommandHandler> logger)
    {
        _prospectRepo = prospectRepo;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<bool> Handle(RejectPendingProspectCommand command, string userId, CancellationToken ct = default)
    {
        if (command.ProspectId == Guid.Empty) throw new InvalidOperationException("Invalid prospect ID");
        var prospect = await _prospectRepo.GetByIdAsync(command.ProspectId, ct);
        if (prospect == null) return false;
        if (!prospect.IsPending) throw new InvalidOperationException("Can only reject pending prospects");
        if (!string.IsNullOrWhiteSpace(prospect.OwnerId) && prospect.OwnerId != userId)
            throw new UnauthorizedAccessException("You are not allowed to reject this prospect.");

        var prospectName = prospect.Name;
        await _prospectRepo.DeleteAsync(command.ProspectId, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        _logger.LogInformation("Rejected and deleted pending prospect: '{ProspectName}' (ID: {ProspectId})", prospectName, command.ProspectId);
        return true;
    }
}
