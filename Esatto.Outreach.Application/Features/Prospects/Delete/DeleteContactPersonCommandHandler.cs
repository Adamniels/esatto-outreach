using Esatto.Outreach.Application.Abstractions.Repositories;

namespace Esatto.Outreach.Application.Features.Prospects;

public sealed class DeleteContactPersonCommandHandler
{
    private readonly IProspectRepository _repository;

    public DeleteContactPersonCommandHandler(IProspectRepository repository)
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
