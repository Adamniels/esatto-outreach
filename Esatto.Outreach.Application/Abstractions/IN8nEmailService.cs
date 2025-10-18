using Esatto.Outreach.Application.DTOs;

namespace Esatto.Outreach.Application.Abstractions;

public interface IN8nEmailService
{
    Task<ResponseSendOutreachToN8nDTO> SendEmailAsync(
        SendOutreachToN8nDTO request,
        CancellationToken cancellationToken = default);
}
