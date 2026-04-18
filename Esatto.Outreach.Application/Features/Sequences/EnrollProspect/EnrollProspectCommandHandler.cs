using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Application.Features.Sequences.Shared;

namespace Esatto.Outreach.Application.Features.Sequences.EnrollProspect;

public class EnrollProspectCommandHandler
{
    private readonly ISequenceRepository _sequenceRepo;
    private readonly IProspectRepository _prospectRepo;
    private readonly SequenceAccessCommandHandler _access;

    public EnrollProspectCommandHandler(ISequenceRepository sequenceRepo, IProspectRepository prospectRepo, SequenceAccessCommandHandler access)
    {
        _sequenceRepo = sequenceRepo;
        _prospectRepo = prospectRepo;
        _access = access;
    }

    public async Task<SequenceProspectViewDto> Handle(EnrollProspectCommand command, string userId, CancellationToken ct = default)
    {
        var sequence = await _access.GetOwnedWithDetailsAsync(command.SequenceId, userId, ct);

        var prospect = await _prospectRepo.GetByIdAsync(command.ProspectId, ct);
        if (prospect == null)
            throw new KeyNotFoundException("Prospect not found");

        if (prospect.OwnerId != userId)
            throw new UnauthorizedAccessException("You don't have permission to use this prospect");

        if (!prospect.ContactPersons.Any(c => c.Id == command.ContactPersonId))
            throw new KeyNotFoundException("Contact person not found on this prospect");

        var sequenceProspect = sequence.EnrollProspect(command.ProspectId, command.ContactPersonId);

        await _sequenceRepo.AddProspectAsync(sequenceProspect, ct);

        var fullSP = await _sequenceRepo.GetProspectExecutionDetailsAsync(sequenceProspect.Id, ct);

        return SequenceProspectViewDto.FromEntity(fullSP!);
    }
}
