using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Application.DTOs.Prospects;

namespace Esatto.Outreach.Application.UseCases.Prospects;

/// <summary>
/// Gets the currently active contact for a prospect.
/// </summary>
public class GetActiveContact
{
    private readonly IProspectRepository _repository;

    public GetActiveContact(IProspectRepository repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// Retrieves the active contact for the specified prospect.
    /// </summary>
    /// <param name="prospectId">ID of the prospect</param>
    /// <param name="userId">ID of the user making the request (must be owner)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>ContactPersonDto if active contact exists, null otherwise</returns>
    /// <exception cref="UnauthorizedAccessException">Thrown if user doesn't own the prospect</exception>
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
        
        var activeContact = prospect.GetActiveContact();
        
        return activeContact != null 
            ? ContactPersonDto.FromEntity(activeContact) 
            : null;
    }
}
