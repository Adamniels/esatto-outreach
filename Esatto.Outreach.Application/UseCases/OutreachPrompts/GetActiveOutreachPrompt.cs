using Esatto.Outreach.Application.Abstractions;
using Esatto.Outreach.Application.DTOs;

using Esatto.Outreach.Domain.Enums;

namespace Esatto.Outreach.Application.UseCases.OutreachPrompts;

public sealed class GetActiveOutreachPrompt
{
    private readonly IOutreachPromptRepository _repo;

    public GetActiveOutreachPrompt(IOutreachPromptRepository repo) => _repo = repo;

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
