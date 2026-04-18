using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Application.Features.Prospects.Shared;
using Esatto.Outreach.Domain.Entities;

namespace Esatto.Outreach.Application.Features.Prospects.AddContactPerson;

public class AddContactPersonCommandHandler
{
    private readonly IProspectRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public AddContactPersonCommandHandler(IProspectRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ContactPersonDto?> Handle(AddContactPersonCommand command, string userId, CancellationToken ct = default)
    {
        var prospect = await _repository.GetByIdAsync(command.ProspectId, ct);
        if (prospect is null) return null;
        if (prospect.OwnerId != userId) throw new UnauthorizedAccessException("You are not allowed to modify this prospect.");

        if (string.IsNullOrWhiteSpace(command.Name))
            throw new ArgumentException("Name is required");

        var existing = prospect.ContactPersons.FirstOrDefault(c => c.Name.Equals(command.Name, StringComparison.OrdinalIgnoreCase));
        if (existing != null)
            throw new InvalidOperationException($"Contact person '{command.Name}' already exists. Use the update endpoint instead.");

        var person = ContactPerson.Create(
            prospect.Id,
            command.Name,
            command.Title,
            command.Email,
            command.LinkedInUrl
        );

        await _repository.AddContactPersonAsync(person, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return ContactPersonDto.FromEntity(person);
    }
}
