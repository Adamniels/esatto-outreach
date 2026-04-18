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

    public async Task<ContactPersonDto?> Handle(Guid prospectId, Guid contactId, UpdateContactPersonRequest request, CancellationToken ct = default)
    {
        var prospect = await _repository.GetByIdAsync(prospectId, ct);
        if (prospect is null) return null;

        var contact = prospect.ContactPersons.FirstOrDefault(c => c.Id == contactId);
        if (contact is null) return null;

        contact.UpdateDetails(request.Name, request.Title, request.Email, request.LinkedInUrl);
        contact.UpdateEnrichment(request.PersonalHooks, request.PersonalNews, request.GeneralInfo);

        await _repository.UpdateContactPersonAsync(contact, ct);
        return ContactPersonDto.FromEntity(contact);
    }
}
