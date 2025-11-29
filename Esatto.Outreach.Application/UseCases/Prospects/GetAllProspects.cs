using Esatto.Outreach.Application.Abstractions;
using Esatto.Outreach.Application.DTOs;

namespace Esatto.Outreach.Application.UseCases.Prospects;

public class GetAllProspects
{
    private readonly IProspectRepository _repository;

    public GetAllProspects(IProspectRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<ProspectViewDto>> ExecuteAsync(CancellationToken ct = default)
    {
        var prospects = await _repository.ListAsync(ct);
        return prospects.Select(ProspectViewDto.FromEntity).ToList();
    }
}
