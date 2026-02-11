namespace Esatto.Outreach.Application.Abstractions;

public interface IEmailSender
{
    Task SendEmailAsync(string to, string subject, string body, CancellationToken cancellationToken = default);
}

public interface ILinkedInActionsClient
{
    Task SendMessageAsync(string profileUrl, string message, CancellationToken cancellationToken = default);
    Task SendConnectionRequestAsync(string profileUrl, string message, CancellationToken cancellationToken = default);
    Task PerformInteractionAsync(string profileUrl, string interactionType, CancellationToken cancellationToken = default);
}
