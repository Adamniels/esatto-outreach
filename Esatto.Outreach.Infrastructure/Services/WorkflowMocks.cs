using Esatto.Outreach.Application.Abstractions.Services;
using Microsoft.Extensions.Logging;

namespace Esatto.Outreach.Infrastructure.Services;

public class MockEmailSender : IEmailSender
{
    private readonly ILogger<MockEmailSender> _logger;

    public MockEmailSender(ILogger<MockEmailSender> logger)
    {
        _logger = logger;
    }

    public Task SendEmailAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("MOCK EMAIL SENT to {To}. Subject: {Subject}. Body Length: {BodyLength}", 
            to, subject, body?.Length ?? 0);
        return Task.CompletedTask;
    }
}

public class MockLinkedInClient : ILinkedInActionsClient
{
    private readonly ILogger<MockLinkedInClient> _logger;

    public MockLinkedInClient(ILogger<MockLinkedInClient> logger)
    {
        _logger = logger;
    }

    public Task SendMessageAsync(string profileUrl, string message, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("MOCK LINKEDIN MESSAGE to {ProfileUrl}. Message: {Message}", profileUrl, message);
        return Task.CompletedTask;
    }

    public Task SendConnectionRequestAsync(string profileUrl, string message, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("MOCK LINKEDIN CONNECT to {ProfileUrl}. Note: {Message}", profileUrl, message);
        return Task.CompletedTask;
    }

    public Task PerformInteractionAsync(string profileUrl, string interactionType, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("MOCK LINKEDIN INTERACTION ({Type}) on {ProfileUrl}", interactionType, profileUrl);
        return Task.CompletedTask;
    }
}
