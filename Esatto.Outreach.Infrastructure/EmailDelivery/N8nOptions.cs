namespace Esatto.Outreach.Infrastructure.EmailDelivery;

public class N8nOptions
{
    public string GmailDraftWebhookUrl { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 30;
}
