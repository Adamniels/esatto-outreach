using Esatto.Outreach.Application.Abstractions.Repositories;

namespace Esatto.Outreach.Application.UseCases.Prospects;

/// <summary>
/// Clears the active contact for a prospect (deactivates all contacts).
/// </summary>
public class ClearActiveContact
{
    private readonly IProspectRepository _repository;

    public ClearActiveContact(IProspectRepository repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// Clears the active contact for the specified prospect.
    /// </summary>
    /// <param name="prospectId">ID of the prospect</param>
    /// <param name="userId">ID of the user making the request (must be owner)</param>
    /// <param name="ct">Cancellation token</param>
    /// <exception cref="InvalidOperationException">Thrown if prospect not found</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown if user doesn't own the prospect</exception>
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
        
        prospect.ClearActiveContact();
        
        await _repository.UpdateAsync(prospect, ct);
    }
}
