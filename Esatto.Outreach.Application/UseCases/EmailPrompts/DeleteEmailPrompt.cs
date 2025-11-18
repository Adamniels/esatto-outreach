using Esatto.Outreach.Application.Abstractions;

namespace Esatto.Outreach.Application.UseCases.EmailPrompts;

public sealed class DeleteEmailPrompt
{
    private readonly IGenerateEmailPromptRepository _repo;

    public DeleteEmailPrompt(IGenerateEmailPromptRepository repo) => _repo = repo;

    public async Task<bool> Handle(Guid id, CancellationToken ct = default)
    {
        var prompt = await _repo.GetByIdAsync(id, ct);
        if (prompt == null)
            return false;

        // Don't allow deleting the active prompt
        if (prompt.IsActive)
            throw new InvalidOperationException("Cannot delete the active prompt. Activate another prompt first.");

        await _repo.DeleteAsync(id, ct);
        return true;
    }
}
