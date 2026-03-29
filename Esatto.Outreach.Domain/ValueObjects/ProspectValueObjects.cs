namespace Esatto.Outreach.Domain.ValueObjects;

/// <summary>
/// Generic value objects for prospect data that can come from any CRM or manual entry.
/// These replace the Capsule-specific types to support multi-CRM scenarios.
/// </summary>

/// <summary>
/// Represents a website URL with optional service and type metadata.
/// </summary>
public record Website(
    string Url,
    string? Service,
    string? Type)
{
    public static Website Create(string url, string? service = null, string? type = null)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("Website URL cannot be empty", nameof(url));

        return new Website(url, service, type);
    }
}

/// <summary>
/// Represents a tag or label applied to a prospect.
/// Id is nullable to support manual tags without external CRM IDs.
/// </summary>
public record Tag(
    long? Id,
    string Name,
    bool DataTag = false)
{
    public static Tag Create(string name, long? id = null, bool dataTag = false)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Tag name cannot be empty", nameof(name));

        return new Tag(id, name, dataTag);
    }
}

/// <summary>
/// Represents a custom field with key-value data.
/// Structure supports CRM-specific field definitions while remaining generic.
/// </summary>
public record CustomField(
    long? Id,
    string? FieldName,
    long? FieldDefinitionId,
    string? Value,
    long? TagId)
{
    public static CustomField Create(
        string? fieldName,
        string? value,
        long? id = null,
        long? fieldDefinitionId = null,
        long? tagId = null)
    {
        return new CustomField(id, fieldName, fieldDefinitionId, value, tagId);
    }
}
