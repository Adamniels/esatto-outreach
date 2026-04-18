using Esatto.Outreach.Application.Abstractions.Repositories;

namespace Esatto.Outreach.Application.Features.Prospects;

public class DeleteProspectCommandHandler
{
    private readonly IProspectRepository _repository;

    public DeleteProspectCommandHandler(IProspectRepository repository)
    {
        _repository = repository;
    }

    public async Task<bool> Handle(Guid id, string userId, CancellationToken ct = default)
    {
        var prospect = await _repository.GetByIdAsync(id, ct);
        if (prospect == null)
            return false;

        // Ownership check - only owner can delete
        if (prospect.OwnerId != userId)
            throw new UnauthorizedAccessException("You don't have permission to delete this prospect");

        await _repository.DeleteAsync(id, ct);
        return true;
    }
}
