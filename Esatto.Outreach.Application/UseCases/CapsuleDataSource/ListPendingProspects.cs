using Esatto.Outreach.Application.Abstractions;
using Esatto.Outreach.Application.DTOs;
using Microsoft.Extensions.Logging;

namespace Esatto.Outreach.Application.UseCases.CapsuleDataSource;

/// <summary>
/// Listar alla pending prospects från Capsule CRM.
/// </summary>
public class ListPendingProspects
{
    private readonly IProspectRepository _prospectRepo;
    private readonly ILogger<ListPendingProspects> _logger;

    public ListPendingProspects(
        IProspectRepository prospectRepo,
        ILogger<ListPendingProspects> logger)
    {
        _prospectRepo = prospectRepo;
        _logger = logger;
    }

    public async Task<List<PendingProspectDto>> Handle(CancellationToken ct = default)
    {
        var pendingProspects = await _prospectRepo.ListPendingAsync(ct);

        _logger.LogInformation("Found {Count} pending prospects", pendingProspects.Count);

        return pendingProspects
            .Select(PendingProspectDto.FromEntity)
            .ToList();
    }
}
