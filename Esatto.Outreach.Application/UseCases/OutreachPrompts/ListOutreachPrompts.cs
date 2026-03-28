using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Application.DTOs.Outreach;

namespace Esatto.Outreach.Application.UseCases.OutreachPrompts;

public sealed class ListOutreachPrompts
{
    private readonly IOutreachPromptRepository _repo;

    public ListOutreachPrompts(IOutreachPromptRepository repo) => _repo = repo;

    public async Task<IReadOnlyList<OutreachPromptDto>> Handle(string userId, CancellationToken ct = default)
    {
        var prompts = await _repo.ListByUserIdAsync(userId, ct);
        return prompts.Select(p => new OutreachPromptDto(
            p.Id,
            p.Instructions,
            p.Type,
            p.IsActive,
            p.CreatedUtc,
            p.UpdatedUtc
        )).ToList();
    }
}
