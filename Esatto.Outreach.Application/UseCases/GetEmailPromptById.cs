using Esatto.Outreach.Application.Abstractions;
using Esatto.Outreach.Application.DTOs;

namespace Esatto.Outreach.Application.UseCases;

public sealed class GetEmailPromptById
{
    private readonly IGenerateEmailPromptRepository _repo;

    public GetEmailPromptById(IGenerateEmailPromptRepository repo) => _repo = repo;

    public async Task<EmailPromptDto?> Handle(Guid id, CancellationToken ct = default)
    {
        var prompt = await _repo.GetByIdAsync(id, ct);
        if (prompt == null) return null;
        
        return new EmailPromptDto(
            prompt.Id,
            prompt.Instructions,
            prompt.IsActive,
            prompt.CreatedUtc,
            prompt.UpdatedUtc
        );
    }
}
