using Esatto.Outreach.Application.Abstractions.Repositories;

namespace Esatto.Outreach.Application.Features.Prospects.SetActiveContact;

public class SetActiveContactCommandHandler
{
    private readonly IProspectRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public SetActiveContactCommandHandler(IProspectRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(SetActiveContactCommand command, string userId, CancellationToken ct = default)
    {
        var prospect = await _repository.GetByIdAsync(command.ProspectId, ct);

        if (prospect == null)
            throw new InvalidOperationException($"Prospect with ID {command.ProspectId} not found");

        if (prospect.OwnerId != userId)
            throw new UnauthorizedAccessException("You are not authorized to modify this prospect");

        prospect.SetActiveContact(command.ContactPersonId);
        await _repository.UpdateAsync(prospect, ct);
        await _unitOfWork.SaveChangesAsync(ct);
    }
}
