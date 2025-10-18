namespace Esatto.Outreach.Infrastructure.Email;

public class N8nOptions
{
    public string WebhookUrl { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 30;
}
