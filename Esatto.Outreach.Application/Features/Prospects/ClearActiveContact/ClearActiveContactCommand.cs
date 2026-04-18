namespace Esatto.Outreach.Application.Features.Prospects.ClearActiveContact;

public sealed record ClearActiveContactCommand(
    Guid ProspectId
);
