using Esatto.Outreach.Application.Abstractions;
using Esatto.Outreach.Application.DTOs;
using Esatto.Outreach.Domain.Entities;

namespace Esatto.Outreach.Application.UseCases.Prospects;

public sealed class AddContactPerson
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

        
        // Find the one we just added to set the extra fields if needed and return it
        // Since AddContactPerson in domain is void and simple, we might need to find it again.
        // Or we can manually construct it here using the domain factory to get the object reference.
        // Actually, prospect.AddContactPerson handles the logic. Let's rely on that.
        // But wait, the domain method AddContactPerson doesn't take LinkedInURL. 
        // I should probably update the domain method or do it manually here.
        
        var existing = prospect.ContactPersons.FirstOrDefault(c => c.Name.Equals(dto.Name, StringComparison.OrdinalIgnoreCase));
        if (existing != null)
        {
             existing.UpdateDetails(dto.Name, dto.Title, dto.Email, dto.LinkedInUrl);
             await _repository.UpdateAsync(prospect, ct);
             // When updating existing, we can reuse existing object
        }
        else
        {
             var person = ContactPerson.Create(prospect.Id, dto.Name, dto.Title, dto.Email, dto.LinkedInUrl);
             // prospect.ContactPersons.Add(person); // Don't add to collection manually if we are going to save it explicitly via repo
             // Actually better to add it to collection AND save via explicit add to be safe?
             // If we add to collection, and then call AddContactPersonAsync, EF Fixup handles it.
             
             await _repository.AddContactPersonAsync(person, ct);
             existing = person;
        }

        return ContactPersonDto.FromEntity(existing);
    }
}
