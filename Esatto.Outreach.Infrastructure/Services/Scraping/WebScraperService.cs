using System.Xml.Linq;
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
        var homePage = ParsePageContent(baseUrl, homeHtml);

        // 2. Discover relevant sub-pages
        // Combine Sitemap + Extracted Links, then Apply Strict Filtering
        var sitemapUrls = await GetSitemapUrlsAsync(domain, ct);
        var extractedUrls = ExtractInterestingLinks(homeHtml, baseUrl);
        
        var allCandidateUrls = sitemapUrls.Concat(extractedUrls).Distinct().ToList();
        var filteredUrls = FilterRelevantUrls(allCandidateUrls);

        // Cap at 15 pages for robust analysis
        var urlsToScrape = filteredUrls.Take(15).ToList();

        // 3. Crawl sub-pages
        var subPageTasks = urlsToScrape.Select(async url => 
        {
            var html = await GetHtmlContentAsync(url, ct);
            return ParsePageContent(url, html);
        });

        var subPages = await Task.WhenAll(subPageTasks);

        // 4. Aggregate
        var allPages = new List<WebPageContent> { homePage };
        allPages.AddRange(subPages.Where(p => !string.IsNullOrWhiteSpace(p.BodyText))); // Filter out failed/empty scrapes

        // Legacy consolidated text (can be deprecated later or used for fallback)
        var consolidatedText = string.Join("\n\n--- PAGE START ---\n", allPages.Select(p => $"Title: {p.Title}\nH1: {p.H1}\nURL: {p.Url}\n{p.BodyText}"));
        if (consolidatedText.Length > 50000) consolidatedText = consolidatedText.Substring(0, 50000); // Higher limit

        return new ScrapedSiteData(baseUrl, homePage.Title, "", consolidatedText, allCandidateUrls, allPages);
    }

    public async Task<List<string>> GetSitemapUrlsAsync(string domain, CancellationToken ct = default)
    {
        try
        {
            var baseUrl = domain.StartsWith("http") ? domain : $"https://{domain}";
            var sitemapUrl = $"{baseUrl}/sitemap.xml";

            var xml = await _httpClient.GetStringAsync(sitemapUrl, ct);
            var doc = XDocument.Parse(xml);
            XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";

            return doc.Descendants(ns + "loc")
                          .Select(x => x.Value)
                          .Where(u => !string.IsNullOrWhiteSpace(u))
                          .ToList();
        }
        catch
        {
            return new List<string>();
        }
    }

    public async Task<WebPageContent> ScrapePageAsync(string url, CancellationToken ct = default)
    {
        var html = await GetHtmlContentAsync(url, ct);
        return ParsePageContent(url, html);
    }

    private List<string> FilterRelevantUrls(List<string> urls)
    {
        // Blocklist (from n8n workflow)
        var excludePatterns = new[] 
        {
            "/kontakt", "/contact", "/kontakta-oss", "/jobb", "/karriar", "/career", "/job",
            "/cookies", "/integritet", "/privacy", "/policy", "/404", "/search", "/login",
            "/press", "/nyheter", "/feed", "/wp-json", "/tag/", "/category/", "/author/", "/?"
        };

        // Allowlist (Must keep if matches these, even if potentially blocked, though strictly we prioritize blocklist usually. 
        // The user logic was: Filter OUT exclude, THEN keep if Matches MustKeep OR !Exclude.
        // Simplified: Remove exclude patterns UNLESS it's a specific "About/Case" page that might accidentally match.
        // Actually, the user's logic was:
        // 1. Filter out exclude.
        // 2. Filter keep: (mustKeep empty OR match mustKeep OR !exclude) -> redundant if step 1 did it.
        // Let's stick to a solid Blocklist and then a Relevance Sort.
        
        var relevant = urls.Where(u => !excludePatterns.Any(p => u.Contains(p, StringComparison.OrdinalIgnoreCase)))
                           .Distinct()
                           .ToList();

         // Prioritize "About", "Services", "Case"
         var priorities = new[] { "om-oss", "about", "what-we-do", "case", "work", "tjanster", "services", "losningar", "solutions" };
         
         return relevant.OrderByDescending(u => priorities.Any(p => u.Contains(p, StringComparison.OrdinalIgnoreCase)))
                        .ThenBy(u => u.Length) // Shorter URLs often main pages
                        .ToList();
    }

    private List<string> ExtractInterestingLinks(string html, string baseUrl)
    {
        var links = new HashSet<string>();
        var matches = Regex.Matches(html, @"<a\s+(?:[^>]*?\s+)?href=[""']([^""']*)[""']", RegexOptions.IgnoreCase);
        var uriBase = new Uri(baseUrl);

        foreach (Match match in matches)
        {
            var href = match.Groups[1].Value;
            if (string.IsNullOrWhiteSpace(href) || href.StartsWith("#") || href.StartsWith("mailto:")) continue;

            try 
            {
                var absolute = new Uri(uriBase, href);
                if (absolute.Host == uriBase.Host) links.Add(absolute.ToString());
            }
            catch { }
        }
        return links.ToList();
    }

    private WebPageContent ParsePageContent(string url, string html)
    {
        // Title
        var titleMatch = Regex.Match(html, @"<title>\s*(.+?)\s*</title>", RegexOptions.IgnoreCase);
        var title = titleMatch.Success ? titleMatch.Groups[1].Value.Trim() : "";

        // H1
        var h1Match = Regex.Match(html, @"<h1[^>]*>\s*(.+?)\s*</h1>", RegexOptions.IgnoreCase);
        var h1 = h1Match.Success ? Regex.Replace(h1Match.Groups[1].Value, "<.*?>", "").Trim() : "";

        // Clean Body
        var bodyText = CleanHtmlText(html);

        return new WebPageContent(url, title, h1, bodyText);
    }

    private string CleanHtmlText(string html)
    {
        // 1. Remove Scripts/Styles
        var noScript = Regex.Replace(html, @"<(script|style)[^>]*?>.*?</\1>", "", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        
        // 2. Remove Comments
        var noComments = Regex.Replace(noScript, @"<!--.*?-->", "", RegexOptions.Singleline);

        // 3. Remove Header/Nav/Footer (Simple heuristic)
        // Ideally we'd use a DOM parser, but regex heuristic for common tags:
        // <nav>, <footer>, <header>
        var contentOnly = Regex.Replace(noComments, @"<(nav|footer|header)[^>]*?>.*?</\1>", "", RegexOptions.Singleline | RegexOptions.IgnoreCase);

        // 4. Strip Tags
        var text = Regex.Replace(contentOnly, "<.*?>", " ");
        
        // 5. Decode HTML entities
        text = System.Net.WebUtility.HtmlDecode(text);

        // 6. Aggressive Date Noise Removal (Copyrights, Footer text)
        // This prevents the AI from seeing "© 2025" and thinking the article is from 2025.
        text = Regex.Replace(text, @"(?i)copyright\s+20\d{2}", "");
        text = Regex.Replace(text, @"(?i)©\s*20\d{2}", "");
        text = Regex.Replace(text, @"(?i)all rights reserved", "");
        
        // 7. Normalize Whitespace
        return Regex.Replace(text, @"\s+", " ").Trim();
    }
}
