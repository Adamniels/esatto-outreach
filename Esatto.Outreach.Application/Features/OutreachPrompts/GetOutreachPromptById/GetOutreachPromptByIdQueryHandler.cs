using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Application.Features.OutreachPrompts.Shared;

namespace Esatto.Outreach.Application.Features.OutreachPrompts.GetOutreachPromptById;

public sealed class GetOutreachPromptByIdQueryHandler
{
    private readonly IOutreachPromptRepository _repo;

    public GetOutreachPromptByIdQueryHandler(IOutreachPromptRepository repo) => _repo = repo;

    public async Task<OutreachPromptDto?> Handle(GetOutreachPromptByIdQuery query, string userId, CancellationToken ct = default)
    {
        var prompt = await _repo.GetByIdAsync(query.Id, userId, ct);
        if (prompt == null) return null;

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
