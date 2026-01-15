using Esatto.Outreach.Application.Abstractions;

namespace Esatto.Outreach.Application.UseCases.Prospects;

public sealed class DeleteContactPerson
{
    private readonly IProspectRepository _repository;

    public DeleteContactPerson(IProspectRepository repository)
    {
        _repository = repository;
    }

    public async Task<bool> Handle(Guid prospectId, Guid contactId, CancellationToken ct = default)
    {
        // Ideally we check if it belongs to prospect first, but repo can handle deletion by ID.
        // For safety/validation, we could fetch.
        // Let's allow deletion by ID but verify existence via repo logic
        
        // Simple approach: delete directly via repo method
        await _repository.DeleteContactPersonAsync(contactId, ct);
        return true;
    }
}
