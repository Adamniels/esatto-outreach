using Esatto.Outreach.Domain.Common;
using Esatto.Outreach.Domain.Enums;
using Esatto.Outreach.Domain.ValueObjects;

namespace Esatto.Outreach.Domain.Entities;

public class Prospect : Entity
{
    // === CRM IDENTITY ===
    public CrmProvider CrmSource { get; private set; } = CrmProvider.None;
    public string? ExternalCrmId { get; private set; }
    
    // === CORE DATA ===
    public string Name { get; private set; } = default!;
    public string? About { get; private set; }
    public DateTime? CrmCreatedAt { get; private set; }
    public DateTime? CrmUpdatedAt { get; private set; }
    public DateTime? LastContactedAt { get; private set; }
    public string? PictureURL { get; private set; }
    public bool IsPending { get; private set; } = false;

    // === NESTED COLLECTIONS (JSON columns) ===
    public List<Website> Websites { get; private set; } = new();
    public List<Tag> Tags { get; private set; } = new();
    public List<CustomField> CustomFields { get; private set; } = new();

    public List<ContactPerson> ContactPersons { get; private set; } = new();

    public string? Notes { get; private set; }
    public string? MailTitle { get; private set; }
    public string? MailBodyPlain { get; private set; }
    public string? MailBodyHTML { get; private set; }
    public string? LastOpenAIResponseId { get; private set; }

    // Foreign Key till EntityIntelligence (One-to-One, nullable)
    public Guid? EntityIntelligenceId { get; private set; }

    // Navigation property till EntityIntelligence
    public EntityIntelligence? EntityIntelligence { get; private set; }

    public ProspectStatus Status { get; private set; } = ProspectStatus.New;

    public string? OwnerId { get; private set; }
    public ApplicationUser? Owner { get; private set; }

    // ===============================

    // === HELPER PROPERTIES ===
    public bool IsFromCrm => CrmSource != CrmProvider.None;
    
    /// <summary>
    /// Checks if a field will be overwritten by CRM webhook updates.
    /// </summary>
    public bool WillBeOverwrittenByCrm(string fieldName)
    {
        if (!IsFromCrm) return false;

        var crmFields = new[] { "Name", "About", "Websites",
                                "PictureURL", "CrmUpdatedAt", "LastContactedAt" };
        return crmFields.Contains(fieldName);
    }

    // EF Core kräver parameterlös ctor (protected för att undvika felanvändning)
    protected Prospect() { }

    // === FACTORY METHODS ===

    /// <summary>
    /// Creates a manual prospect (not from any CRM).
    /// </summary>
    public static Prospect CreateManual(
        string name,
        string ownerId,
        List<string>? websiteUrls = null,
        string? notes = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required", nameof(name));

        if (string.IsNullOrWhiteSpace(ownerId))
            throw new ArgumentException("OwnerId is required", nameof(ownerId));

        var prospect = new Prospect
        {
            Name = name.Trim(),
            OwnerId = ownerId,
            Notes = notes,
            IsPending = false,
            CrmSource = CrmProvider.None,
            ExternalCrmId = null,

            // Convert simple strings to value objects
            Websites = websiteUrls?.Select(url => new Website(url, null, null)).ToList() ?? new()
        };

        return prospect;
    }

    /// <summary>
    /// Creates a pending prospect from a CRM system.
    /// The prospect must be claimed by a user before being fully activated.
    /// </summary>
    public static Prospect CreatePendingFromCrm(
        CrmProvider crmSource,
        string externalCrmId,
        string name,
        string? about,
        DateTime crmCreatedAt,
        DateTime crmUpdatedAt,
        DateTime? lastContactedAt,
        string? pictureURL,
        List<Website>? websites = null,
        List<Tag>? tags = null,
        List<CustomField>? customFields = null)
    {
        if (crmSource == CrmProvider.None)
            throw new ArgumentException("CrmSource cannot be None for CRM-imported prospects", nameof(crmSource));

        if (string.IsNullOrWhiteSpace(externalCrmId))
            throw new ArgumentException("ExternalCrmId is required for CRM-imported prospects", nameof(externalCrmId));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required", nameof(name));

        var prospect = new Prospect
        {
            CrmSource = crmSource,
            ExternalCrmId = externalCrmId,
            Name = name.Trim(),
            About = about,
            CrmCreatedAt = crmCreatedAt,
            CrmUpdatedAt = crmUpdatedAt,
            LastContactedAt = lastContactedAt,
            PictureURL = pictureURL,
            Websites = websites ?? new(),
            Tags = tags ?? new(),
            CustomFields = customFields ?? new(),
            IsPending = true,
            OwnerId = null
        };

        return prospect;
    }

    // === UPDATE METHODS ===

    /// <summary>
    /// Updates CRM data from webhook (e.g. party/updated from Capsule).
    /// </summary>
    public void UpdateFromCrm(
        string? name = null,
        string? about = null,
        DateTime? crmUpdatedAt = null,
        DateTime? lastContactedAt = null,
        string? pictureURL = null,
        List<Website>? websites = null,
        List<Tag>? tags = null,
        List<CustomField>? customFields = null)
    {
        if (!IsFromCrm)
            throw new InvalidOperationException("Cannot update non-CRM prospect from CRM data");

        if (name is not null)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Name cannot be empty", nameof(name));
            Name = name.Trim();
        }

        if (about is not null) About = about;
        if (crmUpdatedAt.HasValue) CrmUpdatedAt = crmUpdatedAt.Value;
        if (lastContactedAt.HasValue) LastContactedAt = lastContactedAt;
        if (pictureURL is not null) PictureURL = pictureURL;

        if (websites is not null) Websites = websites;
        if (tags is not null) Tags = tags;
        if (customFields is not null) CustomFields = customFields;

        Touch();
    }

    /// <summary>
    /// Updates manually editable fields (works for both CRM and manual prospects).
    /// NOTE: For CRM prospects, CRM fields may be overwritten on next webhook.
    /// </summary>
    public void UpdateBasics(
        string? name = null,
        List<string>? websiteUrls = null,
        string? notes = null,
        string? mailTitle = null,
        string? mailBodyPlain = null,
        string? mailBodyHTML = null)
    {
        if (name is not null)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Name cannot be empty", nameof(name));
            Name = name.Trim();
        }

        // Convert simple strings to value objects
        if (websiteUrls is not null)
            Websites = websiteUrls.Select(url => new Website(url, null, null)).ToList();

        if (notes is not null) Notes = notes;
        if (mailTitle is not null) MailTitle = mailTitle;
        if (mailBodyPlain is not null) MailBodyPlain = mailBodyPlain;
        if (mailBodyHTML is not null) MailBodyHTML = mailBodyHTML;

        Touch();
    }

    /// <summary>
    /// Claims a pending CRM prospect (first come, first served).
    /// </summary>
    public void Claim(string ownerId)
    {
        if (string.IsNullOrWhiteSpace(ownerId))
            throw new ArgumentException("OwnerId is required", nameof(ownerId));

        if (!IsPending)
            throw new InvalidOperationException("Cannot claim prospect that is not pending");

        if (!IsFromCrm)
            throw new InvalidOperationException("Cannot claim non-CRM prospect");

        OwnerId = ownerId;
        IsPending = false;
        Touch();
    }

    // === HELPER METHODS ===


    public void AddContactPerson(string name, string? title = null, string? email = null)
    {
        if (string.IsNullOrWhiteSpace(name)) return;
        
        ContactPersons ??= new List<ContactPerson>();
        
        // Simple de-dupe by name for now, logic can be refined later
        if (ContactPersons.Any(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            return;

        var person = ContactPerson.Create(Id, name, title, email);
        ContactPersons.Add(person);
        Touch();
    }

    // NOTE: important that primary website is first
    // TODO: maybe i should change so we send all, becuase all it is used for now is as informtion sent to AI
    public string? GetPrimaryWebsite() => Websites?.FirstOrDefault()?.Url;


    public string? GetLinkedInUrl() =>
        Websites?.FirstOrDefault(w =>
            w.Service?.Contains("LinkedIn", StringComparison.OrdinalIgnoreCase) == true ||
            w.Url?.Contains("linkedin.com", StringComparison.OrdinalIgnoreCase) == true)?.Url;

    public void SetStatus(ProspectStatus status)
    {
        Status = status;
        Touch();
    }

    public void SetLastOpenAIResponseId(string? id)
    {
        LastOpenAIResponseId = string.IsNullOrWhiteSpace(id) ? null : id.Trim();
        Touch();
    }

    public void LinkEntityIntelligence(Guid? entityIntelligenceId)
    {
        EntityIntelligenceId = entityIntelligenceId;
        Touch();
    }

    public void UnlinkEntityIntelligence()
    {
        EntityIntelligenceId = null;
        Touch();
    }

    // ========== ACTIVE CONTACT MANAGEMENT ==========

    /// <summary>
    /// Sets the specified contact as active for email generation.
    /// Ensures only one contact can be active at a time by deactivating others.
    /// </summary>
    /// <param name="contactPersonId">ID of the contact to activate</param>
    /// <exception cref="ArgumentException">Thrown when contact not found in this prospect's contacts</exception>
    public void SetActiveContact(Guid contactPersonId)
    {
        var contact = ContactPersons.FirstOrDefault(c => c.Id == contactPersonId);
        if (contact == null)
            throw new ArgumentException($"Contact person with ID {contactPersonId} not found for this prospect", nameof(contactPersonId));
        
        // Deactivate all other contacts
        foreach (var c in ContactPersons.Where(c => c.IsActive && c.Id != contactPersonId))
        {
            c.Deactivate();
        }
        
        // Activate the selected one
        contact.Activate();
        Touch();
    }
    
    /// <summary>
    /// Gets the currently active contact for this prospect.
    /// </summary>
    /// <returns>The active ContactPerson, or null if no contact is active</returns>
    public ContactPerson? GetActiveContact() 
        => ContactPersons.FirstOrDefault(c => c.IsActive);
        
    /// <summary>
    /// Clears the active contact (deactivates all contacts).
    /// </summary>
    public void ClearActiveContact()
    {
        foreach (var c in ContactPersons.Where(c => c.IsActive))
        {
            c.Deactivate();
        }
        Touch();
    }
}

