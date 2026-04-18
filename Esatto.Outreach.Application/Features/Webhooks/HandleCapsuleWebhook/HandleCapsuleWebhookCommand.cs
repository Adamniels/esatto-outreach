using Esatto.Outreach.Application.Features.Webhooks.Shared;

namespace Esatto.Outreach.Application.Features.Webhooks.HandleCapsuleWebhook;

public sealed record HandleCapsuleWebhookCommand(CapsuleWebhookEventDto Webhook);
