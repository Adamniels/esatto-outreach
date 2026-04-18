using Esatto.Outreach.Application.Features.Webhooks.Shared;

namespace Esatto.Outreach.Application.Features.Webhooks.CreateOrUpdateProspectFromCapsule;

public sealed record CreateOrUpdateProspectFromCapsuleCommand(CapsulePartyDto Party);
