using Esatto.Outreach.Domain.Common;

namespace Esatto.Outreach.Domain.Entities;

public class ContactPerson : Entity
{
    public Guid ProspectId { get; private set; }
    public Prospect Prospect { get; private set; } = default!;

    public string Name { get; private set; } = default!;
    public string? Title { get; private set; }
    public string? Email { get; private set; }
    public string? LinkedInUrl { get; private set; }
    
    public List<string> PersonalHooks { get; private set; } = new();
    
    // JSON: List<string> of personal news
    public List<string> PersonalNews { get; private set; } = new();
    
    // A synthesized summary of this person's background/relevance
    public string? Summary { get; private set; }
    
    // When the research was performed
    public DateTime? ResearchedAt { get; private set; }

    protected ContactPerson() { }

    public static ContactPerson Create(
        Guid prospectId, 
        string name, 
        string? title = null, 
        string? email = null, 
        string? linkedInUrl = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required", nameof(name));

        return new ContactPerson
        {
            ProspectId = prospectId,
            Name = name.Trim(),
            Title = title?.Trim(),
            Email = email?.Trim(),
            LinkedInUrl = linkedInUrl?.Trim()
        };
    }

    public void UpdateDetails(string? name, string? title, string? email, string? linkedInUrl)
    {
        if (name is not null)
        {
             if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Name cannot be empty", nameof(name));
            Name = name.Trim();
        }
        
        if (title is not null) Title = title.Trim();
        if (email is not null) Email = email.Trim();
        if (linkedInUrl is not null) LinkedInUrl = linkedInUrl.Trim();
        
        Touch();
    }

    public void UpdateEnrichment(List<string>? personalHooks, List<string>? personalNews, string? summary)
    {
        if (personalHooks is not null) PersonalHooks = personalHooks;
        if (personalNews is not null) PersonalNews = personalNews;
        if (summary is not null) Summary = summary;
        
        ResearchedAt = DateTime.UtcNow;
        Touch();
    }
}
