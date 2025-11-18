namespace Esatto.Outreach.Infrastructure.Common;

// Options används av IOptions<T> för att binda konfigvärden från appsettings/user-secrets.
public sealed class OpenAiOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "gpt-4.1";
    public bool StoreRawOutput { get; set; } = false;
    public bool UseWebSearch { get; set; } = true;
    public double DefaultTemperature { get; set; } = 0.3;
    public int DefaultMaxOutputTokens { get; set; } = 1200;
}
