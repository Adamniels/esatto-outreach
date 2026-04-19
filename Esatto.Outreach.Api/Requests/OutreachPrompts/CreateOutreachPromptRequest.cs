using Esatto.Outreach.Domain.Enums;

namespace Esatto.Outreach.Api.Requests.OutreachPrompts;

public sealed record CreateOutreachPromptRequest(
    string Instructions,
    PromptType Type,
    bool IsActive = false
);
