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
using Esatto.Outreach.Domain.Entities;

using Esatto.Outreach.Domain.Enums;

namespace Esatto.Outreach.Application.UseCases.OutreachPrompts;

public sealed class CreateOutreachPrompt
{
    private readonly IOutreachPromptRepository _repo;

    public CreateOutreachPrompt(IOutreachPromptRepository repo) => _repo = repo;

    public async Task<OutreachPromptDto> Handle(string userId, CreateOutreachPromptDto dto, CancellationToken ct = default)
    {
        var prompt = OutreachPrompt.Create(userId, dto.Instructions, dto.Type, dto.IsActive);
        
        var created = await _repo.AddAsync(prompt, ct);

        return new OutreachPromptDto(
            created.Id,
            created.Instructions,
            created.Type,
            created.IsActive,
            created.CreatedUtc,
            created.UpdatedUtc
        );
    }
}
