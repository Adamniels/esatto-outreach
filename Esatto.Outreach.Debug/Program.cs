using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Esatto.Outreach.Infrastructure;
using Esatto.Outreach.Application.Abstractions;

var builder = Host.CreateApplicationBuilder(args);

// Load .env file manually since we are in a console app and want to keep it simple
    // Search up to 5 levels up
    var current = Directory.GetCurrentDirectory();
    string? envPath = null;
    for (int i = 0; i < 5; i++)
    {
        envPath = Path.Combine(current, ".env");
        if (File.Exists(envPath)) break;
        var parent = Directory.GetParent(current);
        if (parent == null) break;
        current = parent.FullName;
    }
    
    if (!File.Exists(envPath))
    {
        Console.WriteLine("WARNING: .env file NOT FOUND!");
    }

Console.WriteLine($"Loading .env from: {envPath} (Exists: {File.Exists(envPath)})");

if (File.Exists(envPath))
{
    foreach (var line in File.ReadAllLines(envPath))
    {
        var parts = line.Split('=', 2);
        if (parts.Length == 2)
        {
            Environment.SetEnvironmentVariable(parts[0].Trim(), parts[1].Trim());
        }
    }
}

// Configuration
builder.Configuration.AddEnvironmentVariables();
// Ensure key is explicitly set for Options pattern
var apiKey = Environment.GetEnvironmentVariable("OpenAI__ApiKey");
if (!string.IsNullOrEmpty(apiKey))
{
    builder.Configuration["OpenAI:ApiKey"] = apiKey;
}
builder.Configuration["ConnectionStrings:Default"] = "Data Source=debug.db";
builder.Configuration["Database:Provider"] = "Sqlite";
builder.Configuration["Jwt:Secret"] = "EsattoOutreachSuperSecretKeyMinimum32CharactersLong2025!";
builder.Configuration["Jwt:Issuer"] = "EsattoOutreach";
builder.Configuration["Jwt:Audience"] = "EsattoOutreach";
// Map OpenAI key structure if needed, or rely on correct naming in .env or EnvVars
// .env usually has OpenAI__ApiKey=sk-...

builder.Services.AddLogging(logging => 
{
    logging.AddConsole();
    logging.SetMinimumLevel(LogLevel.Debug);
});

// Register Infrastructure
builder.Services.AddInfrastructure(builder.Configuration);

var host = builder.Build();
// ... Setup code remains ...
var mode = args.Length > 0 ? args[0].ToLower() : "full";
Console.WriteLine($"Starting Debug Mode: {mode}");

try 
{
    if (mode == "ddg")
    {
        var serps = host.Services.GetRequiredService<Esatto.Outreach.Infrastructure.Services.Scraping.DuckDuckGoSerpService>();
        var query = "site:linkedin.com/company/ assemblin posts";
        if (args.Length > 1) query = string.Join(" ", args.Skip(1));
        
        Console.WriteLine($"Searching DDG for: '{query}'...");
        var results = await serps.SearchAsync(query, 10);
        
        Console.WriteLine($"Found {results.Count} results:");
        foreach(var r in results)
        {
            Console.WriteLine($"---");
            Console.WriteLine($"Title: {r.Title}");
            Console.WriteLine($"Link:  {r.Link}");
            Console.WriteLine($"Snip:  {r.Snippet}");
        }
    }
    else if (mode == "ai-search")
    {
        var ai = host.Services.GetRequiredService<IOpenAIChatClient>();
        var company = args.Length > 1 ? args[1] : "Esatto AB";
        var prompt = @$"
RESEARCH TASK: Find the 6 LATEST news items or LinkedIn posts for: {company}
CONSTRAINT: Events must be from the LAST 4 MONTHS (Strict).
SOURCES: Prioritize official Press Releases and LinkedIn Company Page posts.
DATE VERIFICATION: Look for explicit dates.

OUTPUT:
Return a JSON ARRAY of objects:
- ""url"": Direct link to the post or article.
- ""date"": YYYY-MM-DD.
- ""title"": Headline or first sentence of post.
- ""summary"": 5-sentence sales context.

RETURN ONLY JSON: [ {{ ""url"": ""..."", ""date"": ""..."", ... }} ]";
        
        Console.WriteLine($"AI Search: {prompt}");
        var (response, _) = await ai.SendChatMessageAsync(prompt, "You are a senior sales researcher.", null, true, 0.1, 1000, null, default);
        
        Console.WriteLine("=== AI RESPONSE ===");
        Console.WriteLine(response.AiMessage);
    }
    else if (mode == "scraper")
    {
        var scraper = host.Services.GetRequiredService<IWebScraperService>();
        var url = args.Length > 1 ? args[1] : "https://example.com";
        Console.WriteLine($"Scraping: {url}");
        var data = await scraper.ScrapePageAsync(url, default);
        Console.WriteLine($"Title: {data.Title}");
        Console.WriteLine($"Body Length: {data.BodyText.Length}");
        Console.WriteLine($"Sample: {data.BodyText.Substring(0, Math.Min(200, data.BodyText.Length))}");
    }
    else if (mode == "full")
    {
        var service = host.Services.GetRequiredService<ICompanyEnrichmentService>();
        var company = args.Length > 1 ? args[1] : "Assemblin";
        var domain = args.Length > 2 ? args[2] : "assemblin.com";
        
        Console.WriteLine($"Starting Full Enrichment for {company} ({domain})...");
        var result = await service.EnrichCompanyAsync(company, domain);
        
        var json = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
        Console.WriteLine("================ RESULT ================");
        Console.WriteLine(json);
        Console.WriteLine("========================================");
        await File.WriteAllTextAsync("debug_robust_enrichment.json", json);
    }
    else 
    {
        Console.WriteLine($"Unknown mode: {mode}. Use 'full', 'ddg', or 'scraper'.");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"ERROR: {ex.Message}");
    Console.WriteLine(ex.StackTrace);
}
