using Esatto.Outreach.Application.Abstractions.Repositories;

namespace Esatto.Outreach.Application.Features.Prospects.DeleteContactPerson;

public sealed class DeleteContactPersonCommandHandler
{
    private readonly IProspectRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteContactPersonCommandHandler(IProspectRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(DeleteContactPersonCommand command, string userId, CancellationToken ct = default)
    {
        var prospect = await _repository.GetByIdAsync(command.ProspectId, ct);
        if (prospect is null) return false;
        if (prospect.OwnerId != userId) throw new UnauthorizedAccessException("You are not allowed to modify this prospect.");

        var contact = prospect.ContactPersons.FirstOrDefault(c => c.Id == command.ContactId);
        if (contact is null) return false;

        await _repository.DeleteContactPersonAsync(command.ContactId, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return true;
    }
}
