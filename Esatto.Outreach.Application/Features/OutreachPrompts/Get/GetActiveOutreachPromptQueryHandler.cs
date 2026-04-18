using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Application.Features.OutreachGeneration;

using Esatto.Outreach.Domain.Enums;

namespace Esatto.Outreach.Application.Features.OutreachPrompts;

public sealed class GetActiveOutreachPromptQueryHandler
{
    private readonly IOutreachPromptRepository _repo;

    public GetActiveOutreachPromptQueryHandler(IOutreachPromptRepository repo) => _repo = repo;

    public async Task<OutreachPromptDto?> Handle(string userId, PromptType type, CancellationToken ct = default)
    {
        var prompt = await _repo.GetActiveByUserIdAndTypeAsync(userId, type, ct);
        
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
