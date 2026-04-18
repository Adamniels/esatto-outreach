using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Application.Features.Prospects.Shared;
using Esatto.Outreach.Domain.Entities;

namespace Esatto.Outreach.Application.Features.Prospects.AddContactPerson;

public class AddContactPersonCommandHandler
{
    private readonly IProspectRepository _repository;

    public AddContactPersonCommandHandler(IProspectRepository repository)
    {
        _repository = repository;
    }

    public async Task<ContactPersonDto?> Handle(Guid prospectId, AddContactPersonRequest request, CancellationToken ct = default)
    {
        var prospect = await _repository.GetByIdAsync(prospectId, ct);
        if (prospect is null) return null;

        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("Name is required");

        var existing = prospect.ContactPersons.FirstOrDefault(c => c.Name.Equals(request.Name, StringComparison.OrdinalIgnoreCase));
        if (existing != null)
            throw new InvalidOperationException($"Contact person '{request.Name}' already exists. Use the update endpoint instead.");

        var person = ContactPerson.Create(
            prospect.Id,
            request.Name,
            request.Title,
            request.Email,
            request.LinkedInUrl
        );

        await _repository.AddContactPersonAsync(person, ct);
        return ContactPersonDto.FromEntity(person);
    }
}
