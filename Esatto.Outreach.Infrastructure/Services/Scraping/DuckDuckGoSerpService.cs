using System.Text.RegularExpressions;
using System.Net;

namespace Esatto.Outreach.Infrastructure.Services.Scraping;

public class DuckDuckGoSerpService
{
    private readonly HttpClient _httpClient;

    public DuckDuckGoSerpService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        // Mimic a real browser to avoid immediate blocking
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
    }

    public async Task<List<SerpResult>> SearchAsync(string query, int maxResults = 5)
    {
        try
        {
            var encodedQuery = WebUtility.UrlEncode(query);
            var url = $"https://html.duckduckgo.com/html/?q={encodedQuery}";
            
            var html = await _httpClient.GetStringAsync(url);
            
            return ParseDdgHtml(html).Take(maxResults).ToList();
        }
        catch (Exception ex)
        {
            // Fail gracefully - SERP scraping is brittle
            Console.WriteLine($"Error scraping DDG: {ex.Message}");
            return new List<SerpResult>();
        }
    }

    private List<SerpResult> ParseDdgHtml(string html)
    {
        var results = new List<SerpResult>();

        // Regex to find result blocks
        // DuckDuckGo HTML structure (approximate):
        // <div class="result ...">
        //   <h2 ...><a class="result__a" href="...">Title</a></h2>
        //   <a class="result__snippet" ...>Snippet</a>
        // </div>

        // We'll use a simplified regex approach to just find links and snippets
        // Look for the "result__a" link which contains the title and URL
        // Then looks for the snippet
        // This is fragile but works for the "html" version usually
        
        var matches = Regex.Matches(html, @"<div[^>]*class=""[^""]*result__body[^""]*""[^>]*>(.*?)</div>", RegexOptions.Singleline);
        
        // If the specific wrapper changes, try a broader search for result__a
        if (matches.Count == 0)
        {
             matches = Regex.Matches(html, @"<a[^>]*class=""[^""]*result__a[^""]*""[^>]*href=""([^""]*)""[^>]*>(.*?)</a>", RegexOptions.Singleline);
             foreach (Match match in matches)
             {
                 var link = WebUtility.HtmlDecode(match.Groups[1].Value);
                 var title = WebUtility.HtmlDecode(Regex.Replace(match.Groups[2].Value, "<.*?>", "")); // Strip tags
                 
                 // Try to capture snippet which usually follows
                 // This is hard with regex on a stream, so we'll just return Title/Link if we can't find snippet easily
                 results.Add(new SerpResult(title, link, ""));
             }
             return results;
        }

        foreach (Match match in matches)
        {
            var content = match.Groups[1].Value;
            
            // Extract Title & Link
            var linkMatch = Regex.Match(content, @"<a[^>]*class=""[^""]*result__a[^""]*""[^>]*href=""([^""]*)""[^>]*>(.*?)</a>", RegexOptions.Singleline);
            var snippetMatch = Regex.Match(content, @"<a[^>]*class=""[^""]*result__snippet[^""]*""[^>]*>(.*?)</a>", RegexOptions.Singleline);

            if (linkMatch.Success)
            {
                var link = WebUtility.HtmlDecode(linkMatch.Groups[1].Value);
                var title = WebUtility.HtmlDecode(Regex.Replace(linkMatch.Groups[2].Value, "<.*?>", ""));
                var snippet = snippetMatch.Success 
                    ? WebUtility.HtmlDecode(Regex.Replace(snippetMatch.Groups[1].Value, "<.*?>", "")) 
                    : "";

                results.Add(new SerpResult(title, link, snippet));
            }
        }

        return results;
    }
}

public record SerpResult(string Title, string Link, string Snippet);
