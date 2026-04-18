namespace Esatto.Outreach.Application.Features.Sequences.SaveBuilderProgress;

public sealed record SaveBuilderProgressCommand(
    Guid Id,
    int CurrentBuilderStep
);
