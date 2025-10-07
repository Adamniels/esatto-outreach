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

    public ProspectStatus Status { get; private set; } = ProspectStatus.New;

    // EF Core kräver parameterlös ctor (protected för att undvika felanvändning)
    protected Prospect() { }

    // Fabriksmetod för att säkerställa invarianten "CompanyName krävs"
    public static Prospect Create(string companyName,
                                  string? domain = null,
                                  string? contactName = null,
                                  string? contactEmail = null,
                                  string? linkedinUrl = null,
                                  string? notes = null)
    {
        if (string.IsNullOrWhiteSpace(companyName))
            throw new ArgumentException("CompanyName is required", nameof(companyName));

        var p = new Prospect
        {
            CompanyName = companyName.Trim(),
            Domain = domain?.Trim(),
            ContactName = contactName?.Trim(),
            ContactEmail = contactEmail?.Trim(),
            LinkedinUrl = linkedinUrl?.Trim(),
            Notes = notes
        };

        return p;
    }

    // Enkla uppdateringar (sätter UpdatedUtc via Touch)
    public void UpdateBasics(string? companyName = null,
                             string? domain = null,
                             string? contactName = null,
                             string? contactEmail = null,
                             string? linkedinUrl = null,
                             string? notes = null)
    {
        if (!string.IsNullOrWhiteSpace(companyName))
            CompanyName = companyName.Trim();

        Domain = domain?.Trim();
        ContactName = contactName?.Trim();
        ContactEmail = contactEmail?.Trim();
        LinkedinUrl = linkedinUrl?.Trim();
        Notes = notes;

        Touch();
    }

    public void SetStatus(ProspectStatus status)
    {
        Status = status;
        Touch();
    }
}

