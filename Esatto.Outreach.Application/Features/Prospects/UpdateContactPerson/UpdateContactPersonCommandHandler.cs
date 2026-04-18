using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Application.Features.Prospects.Shared;

namespace Esatto.Outreach.Application.Features.Prospects.UpdateContactPerson;

public sealed class UpdateContactPersonCommandHandler
{
    private readonly IProspectRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateContactPersonCommandHandler(IProspectRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ContactPersonDto?> Handle(UpdateContactPersonCommand command, string userId, CancellationToken ct = default)
    {
        var prospect = await _repository.GetByIdAsync(command.ProspectId, ct);
        if (prospect is null) return null;
        if (prospect.OwnerId != userId) throw new UnauthorizedAccessException("You are not allowed to modify this prospect.");

        var contact = prospect.ContactPersons.FirstOrDefault(c => c.Id == command.ContactId);
        if (contact is null) return null;

        contact.UpdateDetails(command.Name, command.Title, command.Email, command.LinkedInUrl);
        contact.UpdateEnrichment(command.PersonalHooks, command.PersonalNews, command.GeneralInfo);

        await _repository.UpdateContactPersonAsync(contact, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return ContactPersonDto.FromEntity(contact);
    }
}
