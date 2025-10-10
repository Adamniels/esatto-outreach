using Esatto.Outreach.Application.Abstractions;
using Esatto.Outreach.Application.DTOs;
namespace Esatto.Outreach.Infrastructure.Email;


public class OpenAiCustomEmailGenerator : ICustomEmailGenerator
{
    Task<CustomEmailDraftDto> GenerateAsync(CustomEmailRequestDto request, CancellationToken cancellationToken = default)
    {
        // TODO: jag har ingen anning hur man ska göra denna
        throw new Exception("not implemented yet");
    }
}
