using Esatto.Outreach.Application.Abstractions;
using Esatto.Outreach.Application.DTOs;

namespace Esatto.Outreach.Application.UseCases.EmailPrompts;

public sealed class ActivateEmailPrompt
{
    private readonly IGenerateEmailPromptRepository _repo;

    public ActivateEmailPrompt(IGenerateEmailPromptRepository repo) => _repo = repo;

    public async Task<EmailPromptDto?> Handle(Guid id, CancellationToken ct = default)
    {
        var prompt = await _repo.GetByIdAsync(id, ct);
        if (prompt == null)
            return null;

        // Deactivate all other prompts first
        await _repo.DeactivateAllAsync(ct);

        // Activate this one
        prompt.Activate();
        await _repo.UpdateAsync(prompt, ct);

        return new EmailPromptDto(
            prompt.Id,
            prompt.Instructions,
            prompt.IsActive,
            prompt.CreatedUtc,
            prompt.UpdatedUtc
        );
    }
}
