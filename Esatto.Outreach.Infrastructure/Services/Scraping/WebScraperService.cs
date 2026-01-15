using System.Text.RegularExpressions;
using Esatto.Outreach.Application.Abstractions;

namespace Esatto.Outreach.Infrastructure.Services.Scraping;

public class WebScraperService : IWebScraperService
{
    private readonly HttpClient _httpClient;

    public WebScraperService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");
    }

    public async Task<string> GetHtmlContentAsync(string url, CancellationToken ct = default)
    {
        try
        {
            return await _httpClient.GetStringAsync(url, ct);
        }
        catch (HttpRequestException ex)
        {
            // Log error
            return $"Error fetching {url}: {ex.Message}";
        }
    }

    public async Task<ScrapedSiteData> ScrapeCompanySiteAsync(string domain, CancellationToken ct = default)
    {
        var baseUrl = domain.StartsWith("http") ? domain : $"https://{domain}";
        
        // 1. Fetch Homepage
        var homeHtml = await GetHtmlContentAsync(baseUrl, ct);
        
        // Extract Metadata from Home
        var titleMatch = Regex.Match(homeHtml, @"<title>\s*(.+?)\s*</title>", RegexOptions.IgnoreCase);
        var title = titleMatch.Success ? titleMatch.Groups[1].Value : domain;

        var descriptionMatch = Regex.Match(homeHtml, @"<meta\s+name=[""']description[""']\s+content=[""'](.+?)[""']", RegexOptions.IgnoreCase);
        var description = descriptionMatch.Success ? descriptionMatch.Groups[1].Value : "";

        // 2. Discover relevant sub-pages (About, Cases, Work)
        var interestingLinks = ExtractInterestingLinks(homeHtml, baseUrl);
        
        // 3. Crawl sub-pages (Max 3)
        var subPageTasks = interestingLinks.Take(3).Select(link => GetHtmlContentAsync(link, ct));
        var subPagesHtml = await Task.WhenAll(subPageTasks);

        // 4. Aggregate Text
        var allHtml = new List<string> { homeHtml };
        allHtml.AddRange(subPagesHtml);

        var consolidatedText = "";
        foreach (var html in allHtml)
        {
            var clean = CleanHtmlText(html);
            if (consolidatedText.Length + clean.Length < 15000)
            {
                consolidatedText += "\n\n" + clean;
            }
            else
            {
                break; // Stop if we hit context limit
            }
        }

        return new ScrapedSiteData(baseUrl, title, description, consolidatedText.Trim(), interestingLinks);
    }

    private List<string> ExtractInterestingLinks(string html, string baseUrl)
    {
        var links = new HashSet<string>();
        // Regex to find <a href="...">
        var matches = Regex.Matches(html, @"<a\s+(?:[^>]*?\s+)?href=[""']([^""']*)[""']", RegexOptions.IgnoreCase);

        // Keywords for pages we want (Swedish & English)
        var keywords = new[] { "about", "om-oss", "om oss", "work", "case", "kund", "referen", "project", "uppdrag" };

        var uriBase = new Uri(baseUrl);

        foreach (Match match in matches)
        {
            var href = match.Groups[1].Value;
            if (string.IsNullOrWhiteSpace(href) || href.StartsWith("#") || href.StartsWith("mailto:") || href.StartsWith("tel:")) 
                continue;

            try 
            {
                var absoluteUri = new Uri(uriBase, href);
                // Only internal links
                if (absoluteUri.Host != uriBase.Host) continue;

                var path = absoluteUri.AbsolutePath.ToLower();
                
                if (keywords.Any(k => path.Contains(k)))
                {
                    links.Add(absoluteUri.ToString());
                }
            }
            catch { /* Ignore invalid URIs */ }
        }

        return links.ToList();
    }

    private string CleanHtmlText(string html)
    {
        // Remove style, script
        var noScript = Regex.Replace(html, @"<(script|style)[^>]*?>.*?</\1>", "", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        // Remove tags
        var text = Regex.Replace(noScript, "<.*?>", " ");
        // Normalize whitespace
        return Regex.Replace(text, @"\s+", " ").Trim();
    }
}
