namespace Esatto.Outreach.Domain.ValueObjects;

/// <summary>
/// Represents a tag from Capsule CRM.
/// </summary>
public record CapsuleTag(
    long Id,
    string Name,
    bool DataTag
);
