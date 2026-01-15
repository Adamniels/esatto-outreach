using Esatto.Outreach.Domain.Common;
using Esatto.Outreach.Domain.Enums;
using Esatto.Outreach.Domain.ValueObjects;

namespace Esatto.Outreach.Domain.Entities;

public class Prospect : Entity
{
    // === CAPSULE CRM DATA (nullable - endast om från Capsule) ===
    public long? CapsuleId { get; private set; }
    public string Name { get; private set; } = default!;  // Ersätter CompanyName
    public string? About { get; private set; }
    public DateTime? CapsuleCreatedAt { get; private set; }
    public DateTime? CapsuleUpdatedAt { get; private set; }
    public DateTime? LastContactedAt { get; private set; }
    public string? PictureURL { get; private set; }

    // Nested collections (JSON columns) - tomma listor om manuell prospect
    public List<CapsuleWebsite> Websites { get; private set; } = new();
    public List<CapsuleEmailAddress> EmailAddresses { get; private set; } = new();
    public List<CapsulePhoneNumber> PhoneNumbers { get; private set; } = new();
    public List<CapsuleAddress> Addresses { get; private set; } = new();
    public List<CapsuleTag> Tags { get; private set; } = new();
    public List<CapsuleCustomField> CustomFields { get; private set; } = new();

    // === NEW CONTACT PERSONS ===
    public List<ContactPerson> ContactPersons { get; private set; } = new();

    // === ESATTO WORKFLOW ===
    public bool IsPending { get; private set; } = false;  // Default false för manuella
    public string? Notes { get; private set; }
    public string? MailTitle { get; private set; }
    public string? MailBodyPlain { get; private set; }
    public string? MailBodyHTML { get; private set; }
    public string? LastOpenAIResponseId { get; private set; }

    // Foreign Key till HardCompanyData (One-to-One, nullable)
    public Guid? HardCompanyDataId { get; private set; }

    // Navigation property till HardCompanyData
    public HardCompanyData? HardCompanyData { get; private set; }

    // Foreign Key till EntityIntelligence (One-to-One, nullable)
    public Guid? EntityIntelligenceId { get; private set; }

    // Navigation property till EntityIntelligence
    public EntityIntelligence? EntityIntelligence { get; private set; }

    public ProspectStatus Status { get; private set; } = ProspectStatus.New;

    // ========== OWNERSHIP ==========
    /// <summary>
    /// User who owns this prospect. Nullable för pending Capsule prospects.
    /// </summary>
    public string? OwnerId { get; private set; }

    /// <summary>
    /// Navigation property to owner.
    /// </summary>
    public ApplicationUser? Owner { get; private set; }
    // ===============================

    // === HELPER PROPERTIES ===
    public bool IsFromCapsule => CapsuleId.HasValue;

    // Alla fält KAN ändras manuellt, men CRM-fält kommer skrivas över vid webhook
    public bool WillBeOverwrittenByCapsule(string fieldName)
    {
        if (!IsFromCapsule) return false;

        var crmFields = new[] { "Name", "About", "Websites", "EmailAddresses",
                                "PhoneNumbers", "Addresses", "PictureURL",
                                "CapsuleUpdatedAt", "LastContactedAt" };
        return crmFields.Contains(fieldName);
    }

    // EF Core kräver parameterlös ctor (protected för att undvika felanvändning)
    protected Prospect() { }

    // === FACTORY METHODS ===

    /// <summary>
    /// Skapar en manuell prospect (ej från Capsule CRM).
    /// </summary>
    public static Prospect CreateManual(
        string name,
        string ownerId,
        List<string>? websiteUrls = null,
        List<string>? emailAddresses = null,
        List<string>? phoneNumbers = null,
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
            IsPending = false,  // Manuella prospects är direkt godkända
            CapsuleId = null,   // Inte från Capsule

            // Konvertera enkla strängar till value objects
            Websites = websiteUrls?.Select(url => new CapsuleWebsite(url, null, null)).ToList() ?? new(),
            EmailAddresses = emailAddresses?.Select(email => new CapsuleEmailAddress(email, null)).ToList() ?? new(),
            PhoneNumbers = phoneNumbers?.Select(phone => new CapsulePhoneNumber(phone, null)).ToList() ?? new()
        };

        return prospect;
    }

    /// <summary>
    /// Skapar en pending prospect från Capsule CRM webhook.
    /// </summary>
    public static Prospect CreatePendingFromCapsule(
        long capsuleId,
        string name,
        string? about,
        DateTime capsuleCreatedAt,
        DateTime capsuleUpdatedAt,
        DateTime? lastContactedAt,
        string? pictureURL,
        List<CapsuleAddress>? addresses = null,
        List<CapsulePhoneNumber>? phoneNumbers = null,
        List<CapsuleEmailAddress>? emailAddresses = null,
        List<CapsuleWebsite>? websites = null,
        List<CapsuleTag>? tags = null,
        List<CapsuleCustomField>? customFields = null)
    {
        if (capsuleId <= 0)
            throw new ArgumentException("CapsuleId must be positive", nameof(capsuleId));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required", nameof(name));

        var prospect = new Prospect
        {
            CapsuleId = capsuleId,
            Name = name.Trim(),
            About = about,
            CapsuleCreatedAt = capsuleCreatedAt,
            CapsuleUpdatedAt = capsuleUpdatedAt,
            LastContactedAt = lastContactedAt,
            PictureURL = pictureURL,
            Addresses = addresses ?? new(),
            PhoneNumbers = phoneNumbers ?? new(),
            EmailAddresses = emailAddresses ?? new(),
            Websites = websites ?? new(),
            Tags = tags ?? new(),
            CustomFields = customFields ?? new(),
            IsPending = true,   // Väntar på att claimeas
            OwnerId = null      // Ingen owner förrän claimed
        };

        return prospect;
    }

    // === UPDATE METHODS ===

    /// <summary>
    /// Uppdaterar Capsule CRM-data från webhook (party/updated).
    /// </summary>
    public void UpdateFromCapsule(
        string? name = null,
        string? about = null,
        DateTime? capsuleUpdatedAt = null,
        DateTime? lastContactedAt = null,
        string? pictureURL = null,
        List<CapsuleAddress>? addresses = null,
        List<CapsulePhoneNumber>? phoneNumbers = null,
        List<CapsuleEmailAddress>? emailAddresses = null,
        List<CapsuleWebsite>? websites = null,
        List<CapsuleTag>? tags = null,
        List<CapsuleCustomField>? customFields = null)
    {
        if (!IsFromCapsule)
            throw new InvalidOperationException("Cannot update non-Capsule prospect from Capsule data");

        if (name is not null)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Name cannot be empty", nameof(name));
            Name = name.Trim();
        }

        if (about is not null) About = about;
        if (capsuleUpdatedAt.HasValue) CapsuleUpdatedAt = capsuleUpdatedAt.Value;
        if (lastContactedAt.HasValue) LastContactedAt = lastContactedAt;
        if (pictureURL is not null) PictureURL = pictureURL;

        if (addresses is not null) Addresses = addresses;
        if (phoneNumbers is not null) PhoneNumbers = phoneNumbers;
        if (emailAddresses is not null) EmailAddresses = emailAddresses;
        if (websites is not null) Websites = websites;
        if (tags is not null) Tags = tags;
        if (customFields is not null) CustomFields = customFields;

        Touch();
    }

    /// <summary>
    /// Uppdaterar manuellt redigerbara fält (fungerar för både Capsule och manuella prospects).
    /// OBS: För Capsule prospects kan CRM-fält skrivas över vid nästa webhook.
    /// </summary>
    public void UpdateBasics(
        string? name = null,
        List<string>? websiteUrls = null,
        List<string>? emailAddresses = null,
        List<string>? phoneNumbers = null,
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

        // Konvertera enkla strängar till value objects
        if (websiteUrls is not null)
            Websites = websiteUrls.Select(url => new CapsuleWebsite(url, null, null)).ToList();

        if (emailAddresses is not null)
            EmailAddresses = emailAddresses.Select(email => new CapsuleEmailAddress(email, null)).ToList();

        if (phoneNumbers is not null)
            PhoneNumbers = phoneNumbers.Select(phone => new CapsulePhoneNumber(phone, null)).ToList();

        if (notes is not null) Notes = notes;
        if (mailTitle is not null) MailTitle = mailTitle;
        if (mailBodyPlain is not null) MailBodyPlain = mailBodyPlain;
        if (mailBodyHTML is not null) MailBodyHTML = mailBodyHTML;

        Touch();
    }

    /// <summary>
    /// Claimar en pending Capsule prospect (först till kvarn).
    /// </summary>
    public void Claim(string ownerId)
    {
        if (string.IsNullOrWhiteSpace(ownerId))
            throw new ArgumentException("OwnerId is required", nameof(ownerId));

        if (!IsPending)
            throw new InvalidOperationException("Cannot claim prospect that is not pending");

        if (!IsFromCapsule)
            throw new InvalidOperationException("Cannot claim non-Capsule prospect");

        OwnerId = ownerId;
        IsPending = false;
        Touch();
    }

    // === HELPER METHODS ===

    public void AddEmailAddress(string email, string type = "Work")
    {
        if (string.IsNullOrWhiteSpace(email)) return;
        
        EmailAddresses ??= new List<CapsuleEmailAddress>();
        
        // De-dupe
        if (EmailAddresses.Any(e => e.Address.Equals(email, StringComparison.OrdinalIgnoreCase)))
            return;

        EmailAddresses.Add(new CapsuleEmailAddress(email, type));
        Touch();
    }

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

    public string? GetPrimaryEmail() => EmailAddresses?.FirstOrDefault()?.Address;

    public string? GetWorkEmail() =>
        EmailAddresses?.FirstOrDefault(e => e.Type?.Equals("Work", StringComparison.OrdinalIgnoreCase) == true)?.Address
        ?? GetPrimaryEmail();

    public string? GetPrimaryPhone() => PhoneNumbers?.FirstOrDefault()?.Number;

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

    public void LinkHardCompanyData(Guid? hardCompanyDataId)
    {
        HardCompanyDataId = hardCompanyDataId;
        Touch();
    }

    public void UnlinkHardCompanyData()
    {
        HardCompanyDataId = null;
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
}

