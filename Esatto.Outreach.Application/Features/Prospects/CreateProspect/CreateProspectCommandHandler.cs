using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Application.Features.Prospects.Shared;
using Esatto.Outreach.Domain.Entities;

namespace Esatto.Outreach.Application.Features.Prospects.CreateProspect;

public class CreateProspectCommandHandler
{
    private readonly IProspectRepository _repo;
    private readonly IUnitOfWork _unitOfWork;
    public CreateProspectCommandHandler(IProspectRepository repo, IUnitOfWork unitOfWork)
    {
        _repo = repo;
        _unitOfWork = unitOfWork;
    }

    public async Task<ProspectViewDto> Handle(CreateProspectCommand command, string userId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(command.Name))
            throw new ArgumentException("Name is required");

        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("UserId is required");

        var entity = Prospect.CreateManual(
            name: command.Name,
            ownerId: userId,
            websiteUrls: command.Websites,
            notes: command.Notes
        );

        var saved = await _repo.AddAsync(entity, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return ProspectViewDto.FromEntity(saved);
    }
}
