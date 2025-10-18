using Esatto.Outreach.Application.Abstractions;
using Esatto.Outreach.Application.DTOs;

namespace Esatto.Outreach.Application.UseCases;

public class SendEmailViaN8n
{
    private readonly IProspectRepository _repo;
    private readonly IN8nEmailService _n8nService;

    public SendEmailViaN8n(
        IProspectRepository repo,
        IN8nEmailService n8nService)
    {
        _repo = repo;
        _n8nService = n8nService;
    }

    public async Task<ResponseSendOutreachToN8nDTO> Handle(
        Guid prospectId,
        CancellationToken ct = default)
    {
        // Hämta prospect med sparad email-draft
        var prospect = await _repo.GetByIdAsync(prospectId, ct)
            ?? throw new InvalidOperationException(
                $"Prospect with id {prospectId} not found");

        // Validera att vi har email-data
        if (string.IsNullOrWhiteSpace(prospect.ContactEmail))
            throw new InvalidOperationException("Contact email is missing");

        if (string.IsNullOrWhiteSpace(prospect.MailTitle) ||
            string.IsNullOrWhiteSpace(prospect.MailBodyPlain))
            throw new InvalidOperationException(
                "Email draft not generated. Generate draft first.");

        // Skapa request
        var request = new SendOutreachToN8nDTO(
            To: prospect.ContactEmail,
            Subject: prospect.MailTitle,
            Body: prospect.MailBodyHTML ?? prospect.MailBodyPlain
        );

        // Skicka via n8n
        var result = await _n8nService.SendEmailAsync(request, ct);

        // Uppdatera prospect status om det gick bra (valfritt)
        if (result.Success)
        {
            // Du kan lägga till en metod på Prospect för att markera som skickad
            await _repo.UpdateAsync(prospect, ct);
        }

        return result;
    }
}
