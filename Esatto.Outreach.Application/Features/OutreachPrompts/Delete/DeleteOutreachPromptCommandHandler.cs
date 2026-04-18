using Esatto.Outreach.Application.Abstractions.Repositories;

namespace Esatto.Outreach.Application.Features.OutreachPrompts;

public sealed class DeleteOutreachPromptCommandHandler
{
    private readonly IOutreachPromptRepository _repo;

    public DeleteOutreachPromptCommandHandler(IOutreachPromptRepository repo) => _repo = repo;

    public async Task<bool> Handle(Guid id, string userId, CancellationToken ct = default)
    {
        var prompt = await _repo.GetByIdAsync(id, userId, ct);
        if (prompt == null)
            return false;

        // Don't allow deleting the active prompt
        if (prompt.IsActive)
            throw new InvalidOperationException("Cannot delete the active prompt. Activate another prompt first.");

        await _repo.DeleteAsync(id, userId, ct);
        return true;
    }
}
