using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Application.Features.OutreachPrompts.Shared;

namespace Esatto.Outreach.Application.Features.OutreachPrompts.ActivateOutreachPrompt;

public sealed class ActivateOutreachPromptCommandHandler
{
    private readonly IOutreachPromptRepository _repo;

    public ActivateOutreachPromptCommandHandler(IOutreachPromptRepository repo) => _repo = repo;

    public async Task<OutreachPromptDto?> Handle(ActivateOutreachPromptCommand command, string userId, CancellationToken ct = default)
    {
        var prompt = await _repo.GetByIdAsync(command.Id, userId, ct);
        if (prompt == null)
            return null;

        await _repo.DeactivateAllForUserAndTypeAsync(userId, prompt.Type, ct);
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
