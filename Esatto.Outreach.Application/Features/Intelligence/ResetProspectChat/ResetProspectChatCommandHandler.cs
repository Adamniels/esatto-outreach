using Esatto.Outreach.Application.Abstractions.Repositories;

namespace Esatto.Outreach.Application.Features.Intelligence.ResetProspectChat;

public class ResetProspectChatCommandHandler
{
    private readonly IProspectRepository _repository;

    public ResetProspectChatCommandHandler(IProspectRepository repository)
    {
        _repository = repository;
    }

    public async Task<bool> Handle(Guid prospectId, CancellationToken ct = default)
    {
        var prospect = await _repository.GetByIdAsync(prospectId, ct);
        if (prospect == null)
            return false;

        prospect.SetLastOpenAIResponseId(null);
        await _repository.UpdateAsync(prospect, ct);
        return true;
    }
}
