using Esatto.Outreach.Application.DTOs;
using Microsoft.Extensions.Logging;

namespace Esatto.Outreach.Application.UseCases.CapsuleDataSource;

/// <summary>
/// Orchestrator för Capsule CRM webhooks.
/// Routar olika webhook-typer till rätt use case.
/// </summary>
public class HandleCapsuleWebhook
{
    private readonly CreateOrUpdateProspectFromCapsule _createOrUpdateProspect;
    private readonly ILogger<HandleCapsuleWebhook> _logger;

    public HandleCapsuleWebhook(
        CreateOrUpdateProspectFromCapsule createOrUpdateProspect,
        ILogger<HandleCapsuleWebhook> logger)
    {
        _createOrUpdateProspect = createOrUpdateProspect;
        _logger = logger;
    }

    public async Task<WebhookResultDto> Handle(
        CapsuleWebhookEventDto webhook,
        CancellationToken ct = default)
    {
        if (webhook?.Payload == null || webhook.Payload.Count == 0)
        {
            _logger.LogWarning("Webhook missing payload");
            return new WebhookResultDto(false, "Webhook payload is required");
        }

        var party = webhook.Payload.First();

        // Filtrera bort personer - vi vill bara ha organisationer
        if (party.Type != "organisation")
        {
            _logger.LogInformation(
                "Ignoring party of type '{Type}' (ID: {Id})",
                party.Type,
                party.Id);
            return new WebhookResultDto(true, "Ignored - not an organisation");
        }

        // Validera nödvändig data
        if (string.IsNullOrWhiteSpace(party.Name))
        {
            return new WebhookResultDto(false, "Organisation missing name");
        }

        _logger.LogInformation(
            "Processing {Event} for organisation '{Name}' (Capsule ID: {Id})",
            webhook.Type,
            party.Name,
            party.Id);

        return webhook.Type switch
        {
            // Gör samma sak oavsett om det är update eller create, så att om dem redan har massa prospect på crm så kommer
            // kan dem komma in i systemet om man uppdaterar dem
            "party/created" => await _createOrUpdateProspect.Handle(party, ct),
            "party/updated" => await _createOrUpdateProspect.Handle(party, ct),
            "party/deleted" => HandlePartyDeleted(party),
            _ => new WebhookResultDto(false, $"Unknown event: {webhook.Type}")
        };
    }

    private WebhookResultDto HandlePartyDeleted(CapsulePartyDto party)
    {
        // NOTE: Vi gör ingenting när en party raderas i Capsule
        // Vår prospect behålls för historik
        // vet inte hur jag vill göra i framtiden
        _logger.LogInformation(
            "Ignoring party/deleted for: {Name} (ID: {Id}) - keeping our prospect",
            party.Name,
            party.Id);

        return new WebhookResultDto(true, "Party deletion ignored - prospect kept");
    }
}
