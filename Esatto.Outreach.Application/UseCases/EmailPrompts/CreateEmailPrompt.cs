using Esatto.Outreach.Application.Abstractions;
using Esatto.Outreach.Application.DTOs;
using Esatto.Outreach.Domain.Entities;

namespace Esatto.Outreach.Application.UseCases.EmailPrompts;

public sealed class CreateEmailPrompt
{
    private readonly IGenerateEmailPromptRepository _repo;

    public CreateEmailPrompt(IGenerateEmailPromptRepository repo) => _repo = repo;

    public async Task<EmailPromptDto> Handle(CreateEmailPromptDto dto, CancellationToken ct = default)
    {
        var prompt = GenerateEmailPrompt.Create(dto.Instructions, dto.IsActive);
        
        var created = await _repo.AddAsync(prompt, ct);

        return new EmailPromptDto(
            created.Id,
            created.Instructions,
            created.IsActive,
            created.CreatedUtc,
            created.UpdatedUtc
        );
    }
}
