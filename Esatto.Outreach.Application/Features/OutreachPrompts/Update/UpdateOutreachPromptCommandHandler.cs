using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Application.Features.OutreachGeneration;

namespace Esatto.Outreach.Application.Features.OutreachPrompts;

public sealed class UpdateOutreachPromptCommandHandler
{
    private readonly IOutreachPromptRepository _repo;

    public UpdateOutreachPromptCommandHandler(IOutreachPromptRepository repo) => _repo = repo;

    public async Task<OutreachPromptDto?> Handle(Guid id, string userId, UpdateOutreachPromptDto dto, CancellationToken ct = default)
    {
        var prompt = await _repo.GetByIdAsync(id, userId, ct);
        if (prompt == null)
            return null;

        prompt.UpdateInstructions(dto.Instructions);
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
