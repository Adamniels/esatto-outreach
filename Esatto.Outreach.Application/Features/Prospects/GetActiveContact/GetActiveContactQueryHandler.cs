using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Application.Features.Prospects.Shared;

namespace Esatto.Outreach.Application.Features.Prospects.GetActiveContact;

public class GetActiveContactQueryHandler
{
    private readonly IProspectRepository _repository;

    public GetActiveContactQueryHandler(IProspectRepository repository)
    {
        _repository = repository;
    }

    public async Task<ContactPersonDto?> Handle(
        Guid prospectId,
        string userId,
        CancellationToken ct = default)
    {
        var prospect = await _repository.GetByIdAsync(prospectId, ct);

        if (prospect == null)
            return null;

        if (prospect.OwnerId != userId)
            throw new UnauthorizedAccessException("You are not authorized to view this prospect");

        var activeContact = prospect.GetActiveContactQueryHandler();

        return activeContact != null
            ? ContactPersonDto.FromEntity(activeContact)
            : null;
    }
}
