using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Application.DTOs.Sequence;
using Esatto.Outreach.Domain.Entities.SequenceFeature;
using Esatto.Outreach.Domain.Enums;

namespace Esatto.Outreach.Application.UseCases.Sequences;

public class EnrollProspect
{
    private readonly ISequenceRepository _sequenceRepo;
    private readonly IProspectRepository _prospectRepo;

    public EnrollProspect(ISequenceRepository sequenceRepo, IProspectRepository prospectRepo)
    {
        _sequenceRepo = sequenceRepo;
        _prospectRepo = prospectRepo;
    }

    public async Task<SequenceProspectViewDto> Handle(Guid sequenceId, EnrollProspectRequest request, string userId, CancellationToken ct = default)
    {
        var sequence = await _sequenceRepo.GetByIdWithDetailsAsync(sequenceId, ct);
        if (sequence == null)
            throw new KeyNotFoundException("Sequence not found");

        if (sequence.OwnerId != userId)
            throw new UnauthorizedAccessException("You don't have permission to modify this sequence");

        if (sequence.Status == SequenceStatus.Archived || sequence.Status == SequenceStatus.Completed || sequence.Status == SequenceStatus.Failed)
            throw new InvalidOperationException("You cannot enroll prospects in this sequence because of its current status.");

        if (sequence.Mode == SequenceMode.Focused && sequence.SequenceProspects.Count >= 1)
            throw new InvalidOperationException("Focused sequences can only contain one prospect.");

        if (sequence.SequenceProspects.Any(sp => sp.ProspectId == request.ProspectId))
            throw new InvalidOperationException("This prospect is already enrolled in this sequence.");

        var prospect = await _prospectRepo.GetByIdAsync(request.ProspectId, ct);
        if (prospect == null)
            throw new KeyNotFoundException("Prospect not found");

        if (prospect.OwnerId != userId)
            throw new UnauthorizedAccessException("You don't have permission to use this prospect");

        // Assuming contact exists logic could be augmented, but simply adding it here
        if (!prospect.ContactPersons.Any(c => c.Id == request.ContactPersonId))
            throw new KeyNotFoundException("Contact person not found on this prospect");

        var sequenceProspect = SequenceProspect.Create(sequenceId, request.ProspectId, request.ContactPersonId);
        
        // Setup references explicitly to correctly return full view locally
        sequence.SequenceProspects.Add(sequenceProspect);
        await _sequenceRepo.AddProspectAsync(sequenceProspect, ct);

        // Fetch back with proper include graphs from DB to return complete info
        var fullSP = await _sequenceRepo.GetProspectExecutionDetailsAsync(sequenceProspect.Id, ct);
        
        return SequenceProspectViewDto.FromEntity(fullSP!);
    }
}
