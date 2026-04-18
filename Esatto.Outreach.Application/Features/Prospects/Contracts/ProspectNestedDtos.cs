namespace Esatto.Outreach.Application.Features.Prospects;

public record WebsiteDto(string? Url, string? Service, string? Type);
public record TagDto(long? Id, string Name, bool DataTag);
public record CustomFieldDto(long? Id, string? FieldName, long? FieldDefinitionId, string? Value, long? TagId);
