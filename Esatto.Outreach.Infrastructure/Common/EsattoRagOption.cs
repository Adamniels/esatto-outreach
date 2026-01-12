namespace Esatto.Outreach.Infrastructure.EmailGeneration;

public class EsattoRagOptions
{
    public const string SectionName = "EsattoRag";
    public string BaseUrl { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 30;
    public string? ApiKey { get; set; } // TODO: vill jag ha autentisering
}
