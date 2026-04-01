using Esatto.Outreach.Domain.Enums;

namespace Esatto.Outreach.Domain.Entities.SequenceFeature;

public class SequenceSettings
{
    // Focused mode settings
    public bool? EnrichCompany { get; private set; }
    public bool? EnrichContact { get; private set; }

    // Multi mode settings
    public bool? ResearchSimilarities { get; private set; }
    public int? MaxActiveProspectsPerDay { get; private set; }

    private SequenceSettings() { } // For EF Core

    public static SequenceSettings CreateDefault(SequenceMode mode)
    {
        return mode switch
        {
            SequenceMode.Focused => new SequenceSettings
            {
                EnrichCompany = true,
                EnrichContact = true,
                ResearchSimilarities = null,
                MaxActiveProspectsPerDay = null
            },
            SequenceMode.Multi => new SequenceSettings
            {
                EnrichCompany = null,
                EnrichContact = null,
                ResearchSimilarities = false,
                MaxActiveProspectsPerDay = 20 // Default throttle
            },
            _ => throw new ArgumentException("Invalid sequence mode", nameof(mode))
        };
    }

    // TODO: validation to ensure only relevant settings are updated based on mode
    public void UpdateFocusedSettings(bool enrichCompany, bool enrichContact)
    {
        EnrichCompany = enrichCompany;
        EnrichContact = enrichContact;
    }

    public void UpdateMultiSettings(bool researchSimilarities, int maxActiveProspectsPerDay)
    {
        if (maxActiveProspectsPerDay <= 0)
            throw new ArgumentException("Max active prospects per day must be greater than 0", nameof(maxActiveProspectsPerDay));

        ResearchSimilarities = researchSimilarities;
        MaxActiveProspectsPerDay = maxActiveProspectsPerDay;
    }
}
