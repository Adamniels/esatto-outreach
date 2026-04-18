using Esatto.Outreach.Application.Features.Webhooks.CreateOrUpdateProspectFromCapsule;
using Esatto.Outreach.Application.Features.Webhooks.Shared;
using Microsoft.Extensions.Logging;

namespace Esatto.Outreach.Application.Features.Webhooks.HandleCapsuleWebhook;

public class HandleCapsuleWebhookCommandHandler
{
    private readonly CreateOrUpdateProspectFromCapsuleCommandHandler _createOrUpdateProspect;
    private readonly ILogger<HandleCapsuleWebhookCommandHandler> _logger;

    public HandleCapsuleWebhookCommandHandler(
        CreateOrUpdateProspectFromCapsuleCommandHandler createOrUpdateProspect,
        ILogger<HandleCapsuleWebhookCommandHandler> logger)
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
        if (party.Type != "organisation")
        {
            _logger.LogInformation("Ignoring party of type '{Type}' (ID: {Id})", party.Type, party.Id);
            return new WebhookResultDto(true, "Ignored - not an organisation");
        }

        if (string.IsNullOrWhiteSpace(party.Name))
            return new WebhookResultDto(false, "Organisation missing name");

        _logger.LogInformation("Processing {Event} for organisation '{Name}' (Capsule ID: {Id})", webhook.Type, party.Name, party.Id);

        return webhook.Type switch
        {
            "party/created" => await _createOrUpdateProspect.Handle(party, ct),
            "party/updated" => await _createOrUpdateProspect.Handle(party, ct),
            "party/deleted" => HandlePartyDeleted(party),
            _ => new WebhookResultDto(false, $"Unknown event: {webhook.Type}")
        };
    }

    private WebhookResultDto HandlePartyDeleted(CapsulePartyDto party)
    {
        _logger.LogInformation("Ignoring party/deleted for: {Name} (ID: {Id}) - keeping our prospect", party.Name, party.Id);
        return new WebhookResultDto(true, "Party deletion ignored - prospect kept");
    }
}
