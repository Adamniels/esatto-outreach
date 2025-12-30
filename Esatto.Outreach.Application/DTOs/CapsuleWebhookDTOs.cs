using System.Text.Json.Serialization;

namespace Esatto.Outreach.Application.DTOs;

/// <summary>
/// DTO for Capsule CRM webhook payload.
/// Contains the event type and the party data.
/// </summary>
public record CapsuleWebhookEventDto(
    [property: JsonPropertyName("event")] string Type,  // e.g., "party/created", "party/updated", "party/deleted"
    [property: JsonPropertyName("payload")] List<CapsulePartyDto> Payload
);

/// <summary>
/// DTO representing a Capsule party (organisation or person).
/// </summary>
public record CapsulePartyDto(
    [property: JsonPropertyName("id")] long Id,
    [property: JsonPropertyName("type")] string Type,  // "organisation" or "person"
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("about")] string? About,
    [property: JsonPropertyName("createdAt")] DateTime? CreatedAt,
    [property: JsonPropertyName("updatedAt")] DateTime? UpdatedAt,
    [property: JsonPropertyName("lastContactedAt")] DateTime? LastContactedAt,
    [property: JsonPropertyName("pictureURL")] string? PictureURL,
    [property: JsonPropertyName("websites")] List<CapsuleWebsiteDto>? Websites,
    [property: JsonPropertyName("emailAddresses")] List<CapsuleEmailDto>? EmailAddresses,
    [property: JsonPropertyName("phoneNumbers")] List<CapsulePhoneDto>? PhoneNumbers,
    [property: JsonPropertyName("addresses")] List<CapsuleAddressDto>? Addresses,
    [property: JsonPropertyName("tags")] List<CapsuleTagDto>? Tags,
    [property: JsonPropertyName("fields")] List<CapsuleCustomFieldDto>? Fields
);

/// <summary>
/// Nested DTOs for Capsule party data.
/// </summary>
public record CapsuleWebsiteDto(
    [property: JsonPropertyName("url")] string Url,
    [property: JsonPropertyName("service")] string? Service,
    [property: JsonPropertyName("type")] string? Type
);

public record CapsuleEmailDto(
    [property: JsonPropertyName("address")] string Address,
    [property: JsonPropertyName("type")] string? Type
);

public record CapsulePhoneDto(
    [property: JsonPropertyName("number")] string Number,
    [property: JsonPropertyName("type")] string? Type
);

public record CapsuleAddressDto(
    [property: JsonPropertyName("street")] string? Street,
    [property: JsonPropertyName("city")] string? City,
    [property: JsonPropertyName("state")] string? State,
    [property: JsonPropertyName("zip")] string? Zip,
    [property: JsonPropertyName("country")] string? Country,
    [property: JsonPropertyName("type")] string? Type
);

public record CapsuleTagDto(
    [property: JsonPropertyName("id")] long Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("dataTag")] bool DataTag
);

public record CapsuleCustomFieldDto(
    [property: JsonPropertyName("id")] long Id,
    [property: JsonPropertyName("definition")] CapsuleFieldDefinitionDto? Definition,
    [property: JsonPropertyName("tagId")] long? TagId,
    [property: JsonPropertyName("value")] string? Value
);

public record CapsuleFieldDefinitionDto(
    [property: JsonPropertyName("id")] long Id,
    [property: JsonPropertyName("name")] string Name
);

/// <summary>
/// Result DTO for webhook processing.
/// </summary>
public record WebhookResultDto(
    bool Success,
    string Message
);
