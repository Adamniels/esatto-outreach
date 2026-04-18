using Esatto.Outreach.Application.Abstractions.Repositories;

namespace Esatto.Outreach.Application.Features.Prospects.DeleteProspect;

public class DeleteProspectCommandHandler
{
    private readonly IProspectRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteProspectCommandHandler(IProspectRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(DeleteProspectCommand command, string userId, CancellationToken ct = default)
    {
        var prospect = await _repository.GetByIdAsync(command.Id, ct);
        if (prospect == null)
            return false;

        if (prospect.OwnerId != userId)
            throw new UnauthorizedAccessException("You don't have permission to delete this prospect");

        await _repository.DeleteAsync(command.Id, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return true;
    }
}
