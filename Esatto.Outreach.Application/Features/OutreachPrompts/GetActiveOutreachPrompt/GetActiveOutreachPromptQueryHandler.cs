using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Application.Features.OutreachPrompts.Shared;

namespace Esatto.Outreach.Application.Features.OutreachPrompts.GetActiveOutreachPrompt;

public sealed class GetActiveOutreachPromptQueryHandler
{
    private readonly IOutreachPromptRepository _repo;

    public GetActiveOutreachPromptQueryHandler(IOutreachPromptRepository repo) => _repo = repo;

    public async Task<OutreachPromptDto?> Handle(GetActiveOutreachPromptQuery query, string userId, CancellationToken ct = default)
    {
        var prompt = await _repo.GetActiveByUserIdAndTypeAsync(userId, query.Type, ct);
        if (prompt == null)
            return null;

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
