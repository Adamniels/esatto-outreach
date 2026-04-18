using Esatto.Outreach.Application.Abstractions.Repositories;

namespace Esatto.Outreach.Application.Features.OutreachPrompts.DeleteOutreachPrompt;

public sealed class DeleteOutreachPromptCommandHandler
{
    private readonly IOutreachPromptRepository _repo;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteOutreachPromptCommandHandler(IOutreachPromptRepository repo, IUnitOfWork unitOfWork)
    {
        _repo = repo;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(DeleteOutreachPromptCommand command, string userId, CancellationToken ct = default)
    {
        var prompt = await _repo.GetByIdAsync(command.Id, userId, ct);
        if (prompt == null)
            return false;

        if (prompt.IsActive)
            throw new InvalidOperationException("Cannot delete the active prompt. Activate another prompt first.");

        await _repo.DeleteAsync(command.Id, userId, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return true;
    }
}
