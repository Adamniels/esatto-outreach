using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Application.Features.OutreachGeneration;

namespace Esatto.Outreach.Application.Features.OutreachPrompts;

public sealed class GetOutreachPromptByIdQueryHandler
{
    private readonly IOutreachPromptRepository _repo;

    public GetOutreachPromptByIdQueryHandler(IOutreachPromptRepository repo) => _repo = repo;

    public async Task<OutreachPromptDto?> Handle(Guid id, string userId, CancellationToken ct = default)
    {
        var prompt = await _repo.GetByIdAsync(id, userId, ct);
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
