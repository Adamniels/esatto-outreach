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
    
    /// <summary>
    /// Attempts to find and parse the sitemap to discover high-value pages.
    /// </summary>
    Task<List<string>> GetSitemapUrlsAsync(string domain, CancellationToken ct = default);

    /// <summary>
    /// Scrapes a single specific URL and returns structured content (Title, H1, Body).
    /// </summary>
    Task<WebPageContent> ScrapePageAsync(string url, CancellationToken ct = default);
}

public record ScrapedSiteData(string Url, string Title, string MetaDescription, string BodyText, List<string> Links, List<WebPageContent> Pages);
public record WebPageContent(string Url, string Title, string H1, string BodyText);
