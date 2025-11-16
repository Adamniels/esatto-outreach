using Esatto.Outreach.Application.Abstractions;
using Esatto.Outreach.Application.DTOs;

namespace Esatto.Outreach.Application.UseCases;

public sealed class ListEmailPrompts
{
    private readonly IGenerateEmailPromptRepository _repo;

    public ListEmailPrompts(IGenerateEmailPromptRepository repo) => _repo = repo;

    public async Task<IReadOnlyList<EmailPromptDto>> Handle(CancellationToken ct = default)
    {
        var prompts = await _repo.ListAllAsync(ct);
        return prompts.Select(p => new EmailPromptDto(
            p.Id,
            p.Instructions,
            p.IsActive,
            p.CreatedUtc,
            p.UpdatedUtc
        )).ToList();
    }
}
