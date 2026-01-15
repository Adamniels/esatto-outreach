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
    
    // JSON: List<string> of personal-level hooks/news found for this person
    public string? PersonalHooksJson { get; private set; }
    
    // JSON: List<string> of personal news
    public string? PersonalNewsJson { get; private set; }
    
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

    public void UpdateEnrichment(string? personalHooksJson, string? personalNewsJson, string? summary)
    {
        if (personalHooksJson is not null) PersonalHooksJson = personalHooksJson;
        if (personalNewsJson is not null) PersonalNewsJson = personalNewsJson;
        if (summary is not null) Summary = summary;
        
        ResearchedAt = DateTime.UtcNow;
        Touch();
    }
}
