namespace Esatto.Outreach.Application.Abstractions;

public interface IWebScraperService
{
    /// <summary>
    /// Visits the URL and returns the raw HTML content.
    /// Implementation should handle headless browsing if needed for dynamic sites (SPA).
    /// </summary>
    Task<string> GetHtmlContentAsync(string url, CancellationToken ct = default);
    
    /// <summary>
    /// Visits the company homepage and extracts key metadata useful for AI analysis.
    /// </summary>
    Task<ScrapedSiteData> ScrapeCompanySiteAsync(string domain, CancellationToken ct = default);
}

public record ScrapedSiteData(string Url, string Title, string MetaDescription, string BodyText, List<string> Links);
