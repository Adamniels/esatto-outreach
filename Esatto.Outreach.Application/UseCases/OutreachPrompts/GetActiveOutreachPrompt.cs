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
