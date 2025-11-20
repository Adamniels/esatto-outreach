using Esatto.Outreach.Application.Abstractions;
using Esatto.Outreach.Application.DTOs;

namespace Esatto.Outreach.Application.UseCases.EmailPrompts;

public sealed class UpdateEmailPrompt
{
    private readonly IGenerateEmailPromptRepository _repo;

    public UpdateEmailPrompt(IGenerateEmailPromptRepository repo) => _repo = repo;

    public async Task<EmailPromptDto?> Handle(Guid id, string userId, UpdateEmailPromptDto dto, CancellationToken ct = default)
    {
        var prompt = await _repo.GetByIdAsync(id, userId, ct);
        if (prompt == null)
            return null;

        prompt.UpdateInstructions(dto.Instructions);
        await _repo.UpdateAsync(prompt, ct);

        return new EmailPromptDto(
            prompt.Id,
            prompt.Instructions,
            prompt.IsActive,
            prompt.CreatedUtc,
            prompt.UpdatedUtc
        );
    }
}
