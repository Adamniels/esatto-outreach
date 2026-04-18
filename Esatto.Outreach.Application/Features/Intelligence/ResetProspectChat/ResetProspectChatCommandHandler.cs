using Esatto.Outreach.Application.Abstractions.Repositories;

namespace Esatto.Outreach.Application.Features.Intelligence.ResetProspectChat;

public class ResetProspectChatCommandHandler
{
    private readonly IProspectRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public ResetProspectChatCommandHandler(IProspectRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(ResetProspectChatCommand command, string userId, CancellationToken ct = default)
    {
        var prospect = await _repository.GetByIdAsync(command.ProspectId, ct);
        if (prospect == null)
            return false;
        if (prospect.OwnerId != userId)
            throw new UnauthorizedAccessException("You are not allowed to modify this prospect.");

        prospect.SetLastOpenAIResponseId(null);
        await _repository.UpdateAsync(prospect, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return true;
    }
}
