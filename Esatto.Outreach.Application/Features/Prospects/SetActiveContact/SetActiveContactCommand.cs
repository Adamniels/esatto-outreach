namespace Esatto.Outreach.Application.Features.Prospects.SetActiveContact;

public sealed record SetActiveContactCommand(
    Guid ProspectId,
    Guid ContactPersonId
);
