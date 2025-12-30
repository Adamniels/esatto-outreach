using Esatto.Outreach.Application.Abstractions;

namespace Esatto.Outreach.Application.UseCases.Prospects;

public class DeleteProspect
{
    private readonly IProspectRepository _repository;

    public DeleteProspect(IProspectRepository repository)
    {
        _repository = repository;
    }

    public async Task<bool> ExecuteAsync(Guid id, string userId, CancellationToken ct = default)
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
