using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Application.Features.OutreachPrompts.Shared;
using Esatto.Outreach.Domain.Entities;

namespace Esatto.Outreach.Application.Features.OutreachPrompts.CreateOutreachPrompt;

public sealed class CreateOutreachPromptCommandHandler
{
    private readonly IOutreachPromptRepository _repo;

    public CreateOutreachPromptCommandHandler(IOutreachPromptRepository repo) => _repo = repo;

    public async Task<OutreachPromptDto> Handle(string userId, CreateOutreachPromptRequest dto, CancellationToken ct = default)
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
