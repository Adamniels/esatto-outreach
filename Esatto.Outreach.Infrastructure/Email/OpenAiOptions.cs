namespace Esatto.Outreach.Infrastructure.Email;

// Options används av IOptions<T> för att binda konfigvärden från appsettings/user-secrets.
public sealed class OpenAiOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://api.openai.com";
    public string Model { get; set; } = "gpt-4.1";
    public bool StoreRawOutput { get; set; } = false;
    public bool UseWebSearch { get; set; } = true;
}
