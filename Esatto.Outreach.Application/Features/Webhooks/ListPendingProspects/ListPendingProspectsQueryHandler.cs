using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Application.Features.Prospects.Shared;
using Microsoft.Extensions.Logging;

namespace Esatto.Outreach.Application.Features.Webhooks.ListPendingProspects;

public class ListPendingProspectsQueryHandler
{
    private readonly IProspectRepository _prospectRepo;
    private readonly ILogger<ListPendingProspectsQueryHandler> _logger;

    public ListPendingProspectsQueryHandler(IProspectRepository prospectRepo, ILogger<ListPendingProspectsQueryHandler> logger)
    {
        _prospectRepo = prospectRepo;
        _logger = logger;
    }

    public async Task<List<PendingProspectDto>> Handle(CancellationToken ct = default)
    {
        var pendingProspects = await _prospectRepo.ListPendingAsync(ct);
        _logger.LogInformation("Found {Count} pending prospects", pendingProspects.Count);
        return pendingProspects.Select(PendingProspectDto.FromEntity).ToList();
    }
}
