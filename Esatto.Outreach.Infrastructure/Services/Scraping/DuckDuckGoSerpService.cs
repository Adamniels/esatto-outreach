using System.Text.RegularExpressions;
using System.Net;
using Microsoft.Extensions.Logging;

namespace Esatto.Outreach.Infrastructure.Services.Scraping;

public class DuckDuckGoSerpService
{
    private readonly HttpClient _httpClient;
    private readonly Microsoft.Extensions.Logging.ILogger<DuckDuckGoSerpService> _logger;

    public DuckDuckGoSerpService(HttpClient httpClient, Microsoft.Extensions.Logging.ILogger<DuckDuckGoSerpService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        // Mimic a real browser to avoid immediate blocking
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
    }

    public async Task<List<SerpResult>> SearchAsync(string query, int maxResults = 5)
    {
        try
        {
            _logger.LogInformation("Performing DDG SERP Search for: {Query}", query);
            var encodedQuery = WebUtility.UrlEncode(query);
            var url = $"https://lite.duckduckgo.com/lite/?q={encodedQuery}";
            
            _httpClient.DefaultRequestHeaders.Remove("User-Agent");
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.0 Safari/605.1.15");

            var html = await _httpClient.GetStringAsync(url);
            _logger.LogDebug("DDG HTML fetched ({Length} chars)", html.Length);
            
            var results = ParseDdgLiteHtml(html).Take(maxResults).ToList();
            _logger.LogInformation("DDG SERP Result: Found {Count} results for '{Query}'", results.Count, query);
            
            if (results.Count == 0)
            {
                 _logger.LogWarning("DDG SERP returned 0 results. HTML Preview: {HtmlPreview}...", html.Substring(0, Math.Min(500, html.Length)).Replace("\n", " "));
                 // Dump full HTML for debug
                 await File.WriteAllTextAsync("debug_ddg_error.html", html);
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogWarning("DuckDuckGo Search failed (Timeout/Blocked): {Message}. Returning 0 results.", ex.Message);
            return new List<SerpResult>();
        }
    }

    private List<SerpResult> ParseDdgLiteHtml(string html)
    {
        var results = new List<SerpResult>();

        // DDG Lite Structure:
        // <table ...>
        //   <tr>
        //     <td><a href="...">Title</a></td>
        //   </tr>
        //   <tr>
        //     <td class="result-snippet">Snippet</td>
        //   </tr>
        // </table>

        // We try a simplified regex that captures the Link/Title, then hopefully the next row's snippet.
        // Or we just find all Links, and all Snippets, and zip them.
        
        // Find Links: <a class="result-link" href="...">...</a>
        // Note: DDG Lite uses class="result-link" usually.
        
        var linkMatches = Regex.Matches(html, @"<a[^>]*href=""([^""]*)""[^>]*class=""result-link""[^>]*>(.*?)</a>", RegexOptions.Singleline);
        var snippetMatches = Regex.Matches(html, @"<td[^>]*class=""result-snippet""[^>]*>(.*?)</td>", RegexOptions.Singleline);

        for (int i = 0; i < linkMatches.Count; i++)
        {
            var rawLink = WebUtility.HtmlDecode(linkMatches[i].Groups[1].Value);
            var title = WebUtility.HtmlDecode(Regex.Replace(linkMatches[i].Groups[2].Value, "<.*?>", ""));
            
            var snippet = "";
            if (i < snippetMatches.Count)
            {
                snippet = WebUtility.HtmlDecode(Regex.Replace(snippetMatches[i].Groups[1].Value, "<.*?>", "")).Trim();
            }

            // Decode DDG redirect if present
            var link = rawLink;
            var uddgMatch = Regex.Match(rawLink, @"[?&]uddg=([^&]+)");
            if (uddgMatch.Success) 
            {
                link = WebUtility.UrlDecode(uddgMatch.Groups[1].Value);
            }

            results.Add(new SerpResult(title, link, snippet));
        }

        return results;
    }
}
// Removed SerpResult record as it is defined at the bottom of the file outside the class


public record SerpResult(string Title, string Link, string Snippet);
