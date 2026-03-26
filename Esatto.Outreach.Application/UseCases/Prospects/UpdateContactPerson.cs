using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Application.Abstractions.Services;
using Esatto.Outreach.Application.Abstractions.Clients;
using Esatto.Outreach.Application.DTOs;
using Esatto.Outreach.Application.DTOs.Prospects;
using Esatto.Outreach.Application.DTOs.Auth;
using Esatto.Outreach.Application.DTOs.Intelligence;
using Esatto.Outreach.Application.DTOs.Outreach;
using Esatto.Outreach.Application.DTOs.Webhooks;
using Esatto.Outreach.Application.DTOs.Workflows;
using Esatto.Outreach.Domain.Entities;

namespace Esatto.Outreach.Application.UseCases.Prospects;

public sealed class UpdateContactPerson
{
    private readonly IProspectRepository _repository;

    public UpdateContactPerson(IProspectRepository repository)
    {
        _repository = repository;
    }

    public async Task<ContactPersonDto?> Handle(Guid prospectId, Guid contactId, UpdateContactPersonDto dto, CancellationToken ct = default)
    {
        var prospect = await _repository.GetByIdAsync(prospectId, ct);
        if (prospect is null) return null;

        var contact = prospect.ContactPersons.FirstOrDefault(c => c.Id == contactId);
        if (contact is null) return null;

        contact.UpdateDetails(dto.Name, dto.Title, dto.Email, dto.LinkedInUrl);

        contact.UpdateEnrichment(dto.PersonalHooks, dto.PersonalNews, dto.GeneralInfo);

        await _repository.UpdateContactPersonAsync(contact, ct);

        return ContactPersonDto.FromEntity(contact);
    }
}
