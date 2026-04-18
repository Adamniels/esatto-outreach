using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Application.Features.OutreachPrompts.Shared;

namespace Esatto.Outreach.Application.Features.OutreachPrompts.ListOutreachPrompts;

public sealed class ListOutreachPromptsQueryHandler
{
    private readonly IOutreachPromptRepository _repo;

    public ListOutreachPromptsQueryHandler(IOutreachPromptRepository repo) => _repo = repo;

    public async Task<IReadOnlyList<OutreachPromptDto>> Handle(ListOutreachPromptsQuery query, string userId, CancellationToken ct = default)
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
