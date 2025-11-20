using Esatto.Outreach.Domain.Common;
using Esatto.Outreach.Domain.Enums;

namespace Esatto.Outreach.Domain.Entities;

public class Prospect : Entity
{

    // Minimalt krav: CompanyName
    public string CompanyName { get; private set; } = default!;

    // Frivilliga fält (håll dem som enkla strängar i MVP)
    public string? Domain { get; private set; }          // ex: "example.com" (utan https://)
    public string? ContactName { get; private set; }
    public string? ContactEmail { get; private set; }
    public string? LinkedinUrl { get; private set; }
    public string? Notes { get; private set; }
    public string? MailTitle { get; private set; }
    public string? MailBodyPlain { get; private set; }
    public string? MailBodyHTML { get; private set; }
    public string? LastOpenAIResponseId { get; private set; }

    // Foreign Key till HardCompanyData (One-to-One, nullable)
    public Guid? HardCompanyDataId { get; private set; }
    
    // Navigation property till HardCompanyData
    public HardCompanyData? HardCompanyData { get; private set; }

    // Foreign Key till SoftCompanyData (One-to-One, nullable)
    public Guid? SoftCompanyDataId { get; private set; }
    
    // Navigation property till SoftCompanyData
    public SoftCompanyData? SoftCompanyData { get; private set; }

    public ProspectStatus Status { get; private set; } = ProspectStatus.New;

    // ========== OWNERSHIP ==========
    /// <summary>
    /// User who owns this prospect.
    /// </summary>
    public string OwnerId { get; private set; } = string.Empty;
    
    /// <summary>
    /// Navigation property to owner.
    /// </summary>
    public ApplicationUser Owner { get; private set; } = null!;
    // ===============================

    // EF Core kräver parameterlös ctor (protected för att undvika felanvändning)
    protected Prospect() { }

    // Fabriksmetod för att säkerställa invarianten "CompanyName krävs"
    public static Prospect Create(string companyName,
                                  string ownerId,
                                  string? domain = null,
                                  string? contactName = null,
                                  string? contactEmail = null,
                                  string? linkedinUrl = null,
                                  string? notes = null,
                                  string? mailTitle = null,
                                  string? mailBodyPlain = null,
                                  string? mailBodyHTML = null)
    {
        if (string.IsNullOrWhiteSpace(companyName))
            throw new ArgumentException("CompanyName is required", nameof(companyName));

        if (string.IsNullOrWhiteSpace(ownerId))
            throw new ArgumentException("OwnerId is required", nameof(ownerId));

        var p = new Prospect
        {
            CompanyName = companyName.Trim(),
            OwnerId = ownerId,
            Domain = domain?.Trim(),
            ContactName = contactName?.Trim(),
            ContactEmail = contactEmail?.Trim(),
            LinkedinUrl = linkedinUrl?.Trim(),
            Notes = notes,
            MailTitle = mailTitle,
            MailBodyPlain = mailBodyPlain,
            MailBodyHTML = mailBodyHTML
        };

        return p;
    }

    // Enkla uppdateringar (sätter UpdatedUtc via Touch)
    // Endast fält som skickas in (inte null) uppdateras
    public void UpdateBasics(string? companyName = null,
                             string? domain = null,
                             string? contactName = null,
                             string? contactEmail = null,
                             string? linkedinUrl = null,
                             string? notes = null,
                             string? mailTitle = null,
                             string? mailBodyPlain = null,
                             string? mailBodyHTML = null)
    {
        if (companyName is not null)
            CompanyName = string.IsNullOrWhiteSpace(companyName)
                ? CompanyName  // Behåll befintligt om tomt
                : companyName.Trim();

        if (domain is not null)
            Domain = string.IsNullOrWhiteSpace(domain) ? null : domain.Trim();

        if (contactName is not null)
            ContactName = string.IsNullOrWhiteSpace(contactName) ? null : contactName.Trim();

        if (contactEmail is not null)
            ContactEmail = string.IsNullOrWhiteSpace(contactEmail) ? null : contactEmail.Trim();

        if (linkedinUrl is not null)
            LinkedinUrl = string.IsNullOrWhiteSpace(linkedinUrl) ? null : linkedinUrl.Trim();

        if (notes is not null)
            Notes = notes;

        if (mailTitle is not null)
            MailTitle = mailTitle;

        if (mailBodyPlain is not null)
            MailBodyPlain = mailBodyPlain;

        if (mailBodyHTML is not null)
            MailBodyHTML = mailBodyHTML;

        Touch();
    }

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

    public void LinkSoftCompanyData(Guid? softCompanyDataId)
    {
        SoftCompanyDataId = softCompanyDataId;
        Touch();
    }

    public void UnlinkSoftCompanyData()
    {
        SoftCompanyDataId = null;
        Touch();
    }
}

