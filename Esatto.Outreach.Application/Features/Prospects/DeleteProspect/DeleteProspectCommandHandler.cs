using Esatto.Outreach.Application.Abstractions.Repositories;

namespace Esatto.Outreach.Application.Features.Prospects.DeleteProspect;

public class DeleteProspectCommandHandler
{
    private readonly IProspectRepository _repository;

    public DeleteProspectCommandHandler(IProspectRepository repository)
    {
        _repository = repository;
    }

    public async Task<bool> Handle(DeleteProspectCommand command, string userId, CancellationToken ct = default)
    {
        var prospect = await _repository.GetByIdAsync(command.Id, ct);
        if (prospect == null)
            return false;

        if (prospect.OwnerId != userId)
            throw new UnauthorizedAccessException("You don't have permission to delete this prospect");

        await _repository.DeleteAsync(command.Id, ct);
        return true;
    }
}
