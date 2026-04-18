using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Application.Features.OutreachPrompts.Shared;

namespace Esatto.Outreach.Application.Features.OutreachPrompts.UpdateOutreachPrompt;

public sealed class UpdateOutreachPromptCommandHandler
{
    private readonly IOutreachPromptRepository _repo;

    public UpdateOutreachPromptCommandHandler(IOutreachPromptRepository repo) => _repo = repo;

    public async Task<OutreachPromptDto?> Handle(UpdateOutreachPromptCommand command, string userId, CancellationToken ct = default)
    {
        var prompt = await _repo.GetByIdAsync(command.Id, userId, ct);
        if (prompt == null)
            return null;

        prompt.UpdateInstructions(command.Instructions);
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
