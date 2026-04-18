using Esatto.Outreach.Application.Abstractions.Repositories;

namespace Esatto.Outreach.Application.Features.Prospects.DeleteContactPerson;

public sealed class DeleteContactPersonCommandHandler
{
    private readonly IProspectRepository _repository;

    public DeleteContactPersonCommandHandler(IProspectRepository repository)
    {
        _repository = repository;
    }

    public async Task<bool> Handle(DeleteContactPersonCommand command, CancellationToken ct = default)
    {
        await _repository.DeleteContactPersonAsync(command.ContactId, ct);
        return true;
    }
}
