namespace Esatto.Outreach.Domain.ValueObjects;

/// <summary>
/// Represents a custom field from Capsule CRM.
/// </summary>
public record CapsuleCustomField(
    long Id,
    string? FieldName,           // Definition.Name
    long? FieldDefinitionId,     // Definition.Id
    string? Value,
    long? TagId
);
