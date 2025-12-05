namespace Esatto.Outreach.Domain.ValueObjects;

/// <summary>
/// Value objects för Capsule CRM kontaktinformation.
/// Immutable records som representerar nested data från Capsule.
/// </summary>

public record CapsuleWebsite(
    string Url,
    string? Service,
    string? Type)
{
    public static CapsuleWebsite Create(string url, string? service = null, string? type = null)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("Website URL cannot be empty", nameof(url));

        return new CapsuleWebsite(url, service, type);
    }
}

public record CapsuleEmailAddress(
    string Address,
    string? Type)
{
    public static CapsuleEmailAddress Create(string address, string? type = null)
    {
        if (string.IsNullOrWhiteSpace(address))
            throw new ArgumentException("Email address cannot be empty", nameof(address));

        return new CapsuleEmailAddress(address, type);
    }
}

public record CapsulePhoneNumber(
    string Number,
    string? Type)
{
    public static CapsulePhoneNumber Create(string number, string? type = null)
    {
        if (string.IsNullOrWhiteSpace(number))
            throw new ArgumentException("Phone number cannot be empty", nameof(number));

        return new CapsulePhoneNumber(number, type);
    }
}

public record CapsuleAddress(
    string? Street,
    string? City,
    string? State,
    string? Zip,
    string? Country,
    string? Type)
{
    public static CapsuleAddress Create(
        string? street = null,
        string? city = null,
        string? state = null,
        string? zip = null,
        string? country = null,
        string? type = null)
    {
        return new CapsuleAddress(street, city, state, zip, country, type);
    }
}
