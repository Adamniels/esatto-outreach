using System.Net.Http.Json;
using System.Text.Json;
using Esatto.Outreach.Application.Abstractions;
using Esatto.Outreach.Application.DTOs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Esatto.Outreach.Infrastructure.EmailDelivery;

public class N8nEmailService : IN8nEmailService
{
    private readonly HttpClient _httpClient;
    private readonly N8nOptions _options;

    public N8nEmailService(
        HttpClient httpClient,
        IOptions<N8nOptions> options,
        ILogger<N8nEmailService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<ResponseSendOutreachToN8nDTO> SendEmailAsync(
        SendOutreachToN8nDTO request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var payload = new
            {
                to = request.To,
                subject = request.Subject,
                body = request.Body
            };

            var response = await _httpClient.PostAsJsonAsync(
                _options.GmailDraftWebhookUrl,
                payload,
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return new ResponseSendOutreachToN8nDTO(
                    Success: true,
                    Message: "Email sent successfully");
            }

            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);

            return new ResponseSendOutreachToN8nDTO(
                Success: false,
                Message: $"Failed with status {response.StatusCode}");
        }
        catch (Exception ex)
        {
            return new ResponseSendOutreachToN8nDTO(
                Success: false,
                Message: ex.Message);
        }
    }
}
