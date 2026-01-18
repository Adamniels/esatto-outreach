using Esatto.Outreach.Application.Abstractions;
using Esatto.Outreach.Application.DTOs;
using Esatto.Outreach.Domain.Entities;

namespace Esatto.Outreach.Application.UseCases.Prospects;

public class AddContactPerson
{
    private readonly IProspectRepository _repository;

    public AddContactPerson(IProspectRepository repository)
    {
        _repository = repository;
    }

    public async Task<ContactPersonDto?> Handle(Guid prospectId, CreateContactPersonDto dto, CancellationToken ct = default)
    {
        var prospect = await _repository.GetByIdAsync(prospectId, ct);
        if (prospect is null) return null;

        var existing = prospect.ContactPersons.FirstOrDefault(c => c.Name.Equals(dto.Name, StringComparison.OrdinalIgnoreCase));
        if (existing != null)
        {
            throw new InvalidOperationException($"Contact person '{dto.Name}' already exists. Use the update endpoint instead.");
        }

        // Create the contact person manually
        var person = ContactPerson.Create(
            prospect.Id, 
            dto.Name, 
            dto.Title, 
            dto.Email, 
            dto.LinkedInUrl 
        );
        
        // Add directly to the repository to avoid implicit updates to the Prospect
        await _repository.AddContactPersonAsync(person, ct);

        return ContactPersonDto.FromEntity(person);
    }
}
