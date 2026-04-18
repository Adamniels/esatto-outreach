using Esatto.Outreach.Application.Abstractions.Repositories;

namespace Esatto.Outreach.Application.Features.Prospects;

/// <summary>
/// Sets the specified contact person as the active contact for email generation.
/// Ensures only one contact can be active per prospect.
/// </summary>
public class SetActiveContactCommandHandler
{
    private readonly IProspectRepository _repository;

    public SetActiveContactCommandHandler(IProspectRepository repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// Sets a contact as active for the prospect.
    /// </summary>
    /// <param name="prospectId">ID of the prospect</param>
    /// <param name="contactPersonId">ID of the contact to activate</param>
    /// <param name="userId">ID of the user making the request (must be owner)</param>
    /// <param name="ct">Cancellation token</param>
    /// <exception cref="InvalidOperationException">Thrown if prospect not found</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown if user doesn't own the prospect</exception>
    /// <exception cref="ArgumentException">Thrown if contact not found in prospect's contacts</exception>
    public async Task Handle(
        Guid prospectId,
        Guid contactPersonId,
        string userId,
        CancellationToken ct = default)
    {
        var prospect = await _repository.GetByIdAsync(prospectId, ct);
        
        if (prospect == null)
            throw new InvalidOperationException($"Prospect with ID {prospectId} not found");
            
        if (prospect.OwnerId != userId)
            throw new UnauthorizedAccessException("You are not authorized to modify this prospect");
        
        // Domain logic handles validation and ensures uniqueness
        prospect.SetActiveContactCommandHandler(contactPersonId);
        
        await _repository.UpdateAsync(prospect, ct);
    }
}
