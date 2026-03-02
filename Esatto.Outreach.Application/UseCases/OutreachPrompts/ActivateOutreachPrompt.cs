using Esatto.Outreach.Application.Abstractions;
using Esatto.Outreach.Application.DTOs;

using Esatto.Outreach.Domain.Enums;

namespace Esatto.Outreach.Application.UseCases.OutreachPrompts;

public sealed class ActivateOutreachPrompt
{
    private readonly IOutreachPromptRepository _repo;

    public ActivateOutreachPrompt(IOutreachPromptRepository repo) => _repo = repo;

    public async Task<OutreachPromptDto?> Handle(Guid id, string userId, CancellationToken ct = default)
    {
        var prompt = await _repo.GetByIdAsync(id, userId, ct);
        if (prompt == null)
            return null;

        // Deactivate all other prompts of the same type for this user first
        await _repo.DeactivateAllForUserAndTypeAsync(userId, prompt.Type, ct);

        // Activate this one
        prompt.Activate();
        await _repo.UpdateAsync(prompt, ct);

        return new OutreachPromptDto(
            prompt.Id,
            prompt.Instructions,
            prompt.Type,
            prompt.IsActive,
            prompt.CreatedUtc,
            prompt.UpdatedUtc
        );
    }
}
