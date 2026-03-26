using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Application.Abstractions.Services;
using Esatto.Outreach.Application.Abstractions.Clients;
using Esatto.Outreach.Application.DTOs;
using Esatto.Outreach.Application.DTOs.Prospects;
using Esatto.Outreach.Application.DTOs.Auth;
using Esatto.Outreach.Application.DTOs.Intelligence;
using Esatto.Outreach.Application.DTOs.Outreach;
using Esatto.Outreach.Application.DTOs.Webhooks;
using Esatto.Outreach.Application.DTOs.Workflows;

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
