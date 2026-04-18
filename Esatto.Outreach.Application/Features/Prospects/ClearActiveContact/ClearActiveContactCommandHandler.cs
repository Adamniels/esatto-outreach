using Esatto.Outreach.Application.Abstractions.Repositories;

namespace Esatto.Outreach.Application.Features.Prospects.ClearActiveContact;

public class ClearActiveContactCommandHandler
{
    private readonly IProspectRepository _repository;

    public ClearActiveContactCommandHandler(IProspectRepository repository)
    {
        _repository = repository;
    }

    public async Task Handle(
        Guid prospectId,
        string userId,
        CancellationToken ct = default)
    {
        var prospect = await _repository.GetByIdAsync(prospectId, ct);

        if (prospect == null)
            throw new InvalidOperationException($"Prospect with ID {prospectId} not found");

        if (prospect.OwnerId != userId)
            throw new UnauthorizedAccessException("You are not authorized to modify this prospect");

        prospect.ClearActiveContactCommandHandler();
        await _repository.UpdateAsync(prospect, ct);
    }
}
