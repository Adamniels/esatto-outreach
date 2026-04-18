using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Application.Features.Sequences.Shared;
using Esatto.Outreach.Domain.Entities;
using Esatto.Outreach.Domain.Entities.SequenceFeature;
using Microsoft.AspNetCore.Identity;

namespace Esatto.Outreach.Application.Features.Sequences.CreateSequence;

public class CreateSequenceCommandHandler
{
    private readonly ISequenceRepository _repo;
    private readonly UserManager<ApplicationUser> _userManager;

    public CreateSequenceCommandHandler(ISequenceRepository repo, UserManager<ApplicationUser> userManager)
    {
        _repo = repo;
        _userManager = userManager;
    }

    public async Task<SequenceViewDto> Handle(CreateSequenceCommand command, string userId, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            throw new UnauthorizedAccessException("User not found");

        var sequence = Sequence.Create(
            command.Title,
            command.Description,
            command.Mode,
            userId);

        await _repo.AddAsync(sequence, ct);
        return SequenceViewDto.FromEntity(sequence);
    }
}
