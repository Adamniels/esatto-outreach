using Esatto.Outreach.Application.Abstractions;
using Esatto.Outreach.Application.DTOs;

namespace Esatto.Outreach.Application.UseCases.EmailPrompts;

public sealed class GetActiveEmailPrompt
{
    private readonly IGenerateEmailPromptRepository _repo;

    public GetActiveEmailPrompt(IGenerateEmailPromptRepository repo) => _repo = repo;

    public async Task<EmailPromptDto?> Handle(CancellationToken ct = default)
    {
        var prompt = await _repo.GetActiveAsync(ct);
        
        if (prompt == null)
            return null;

        return new EmailPromptDto(
            prompt.Id,
            prompt.Instructions,
            prompt.IsActive,
            prompt.CreatedUtc,
            prompt.UpdatedUtc
        );
    }
}
