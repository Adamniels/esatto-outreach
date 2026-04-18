using Esatto.Outreach.Application.Abstractions.Repositories;

namespace Esatto.Outreach.Application.Features.Prospects.DeleteContactPerson;

public sealed class DeleteContactPersonCommandHandler
{
    private readonly IProspectRepository _repository;

    public DeleteContactPersonCommandHandler(IProspectRepository repository)
    {
        _repository = repository;
    }

    public async Task<bool> Handle(Guid prospectId, Guid contactId, CancellationToken ct = default)
    {
        await _repository.DeleteContactPersonAsync(contactId, ct);
        return true;
    }
}
