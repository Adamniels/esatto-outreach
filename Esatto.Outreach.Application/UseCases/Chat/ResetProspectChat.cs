using Esatto.Outreach.Application.Abstractions;

namespace Esatto.Outreach.Application.UseCases.Chat;

public class ResetProspectChat
{
    private readonly IProspectRepository _repository;

    public ResetProspectChat(IProspectRepository repository)
    {
        _repository = repository;
    }

    public async Task<bool> ExecuteAsync(Guid prospectId, CancellationToken ct = default)
    {
        var prospect = await _repository.GetByIdAsync(prospectId, ct);
        if (prospect == null)
            return false;

        prospect.SetLastOpenAIResponseId(null);
        await _repository.UpdateAsync(prospect, ct);
        return true;
    }
}
