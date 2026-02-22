namespace Esatto.Outreach.Domain.Enums;

/// <summary>
/// Represents the CRM system that a prospect was imported from.
/// </summary>
public enum CrmProvider
{
    /// <summary>
    /// Manually created prospect, not from any CRM
    /// </summary>
    None = 0,
    
    /// <summary>
    /// Imported from Capsule CRM
    /// </summary>
    Capsule = 1,
    
    // Future CRM providers can be added here:
    // HubSpot = 2,
    // Pipedrive = 3,
    // etc.
}
