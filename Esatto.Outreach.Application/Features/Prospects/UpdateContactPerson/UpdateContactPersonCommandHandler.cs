using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Application.Features.Prospects.Shared;

namespace Esatto.Outreach.Application.Features.Prospects.UpdateContactPerson;

public sealed class UpdateContactPersonCommandHandler
{
    private readonly IProspectRepository _repository;

    public UpdateContactPersonCommandHandler(IProspectRepository repository)
    {
        _repository = repository;
    }

    public async Task<ContactPersonDto?> Handle(UpdateContactPersonCommand command, CancellationToken ct = default)
    {
        var prospect = await _repository.GetByIdAsync(command.ProspectId, ct);
        if (prospect is null) return null;

        var contact = prospect.ContactPersons.FirstOrDefault(c => c.Id == command.ContactId);
        if (contact is null) return null;

        contact.UpdateDetails(command.Name, command.Title, command.Email, command.LinkedInUrl);
        contact.UpdateEnrichment(command.PersonalHooks, command.PersonalNews, command.GeneralInfo);

        await _repository.UpdateContactPersonAsync(contact, ct);
        return ContactPersonDto.FromEntity(contact);
    }
}
