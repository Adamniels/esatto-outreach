using Esatto.Outreach.Application.Abstractions;

namespace Esatto.Outreach.Infrastructure.Services.Enrichment;

public interface ICompanyKnowledgeBaseService
{
    /// <summary>
    /// Analyzes a list of raw web pages to extract structured "knowledge nuggets" (About info, Case Studies).
    /// This acts as the "Reduce" step in the Map-Reduce pipeline.
    /// </summary>
    Task<List<KnowledgeSnippet>> AnalyzePagesAsync(List<WebPageContent> pages, CancellationToken ct = default);
}

public record KnowledgeSnippet
{
    public string SourceUrl { get; init; } = "";
    public string PageTitle { get; init; } = "";
    public string PageType { get; init; } = ""; // "About", "Case", "Service", "Other"
    
    // Extracted content
    public string Summary { get; init; } = "";
    public List<ExtractedCaseStudy> CaseStudies { get; init; } = new();
    public List<string> KeyFacts { get; init; } = new();
}

public record ExtractedCaseStudy(string ClientName, string Challenge, string Solution, string Result);
