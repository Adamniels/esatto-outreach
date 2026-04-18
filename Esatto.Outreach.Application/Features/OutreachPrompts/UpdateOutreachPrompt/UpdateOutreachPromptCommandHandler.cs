using Esatto.Outreach.Application.Abstractions.Repositories;
using Esatto.Outreach.Application.Features.OutreachPrompts.Shared;

namespace Esatto.Outreach.Application.Features.OutreachPrompts.UpdateOutreachPrompt;

public sealed class UpdateOutreachPromptCommandHandler
{
    private readonly IOutreachPromptRepository _repo;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateOutreachPromptCommandHandler(IOutreachPromptRepository repo, IUnitOfWork unitOfWork)
    {
        _repo = repo;
        _unitOfWork = unitOfWork;
    }

    public async Task<OutreachPromptDto?> Handle(UpdateOutreachPromptCommand command, string userId, CancellationToken ct = default)
    {
        var prompt = await _repo.GetByIdAsync(command.Id, userId, ct);
        if (prompt == null)
            return null;

        prompt.UpdateInstructions(command.Instructions);
        await _repo.UpdateAsync(prompt, ct);
        await _unitOfWork.SaveChangesAsync(ct);

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
