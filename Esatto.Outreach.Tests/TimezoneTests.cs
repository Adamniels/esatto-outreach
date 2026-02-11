using System;
using Esatto.Outreach.Domain.Entities;
using Esatto.Outreach.Domain.Enums;
using Xunit;

namespace Esatto.Outreach.Tests;

public class TimezoneTests
{
    [Fact]
    public void Schedule_RespectsTimezone_EasternStandardTime()
    {
        // Assemble
        var prospectId = Guid.NewGuid();
        var instance = WorkflowInstance.Create(prospectId);
        
        // Step: Day 0, 09:00 AM
        instance.AddStep(WorkflowStepType.Email, 0, new TimeSpan(9, 0, 0), ContentGenerationStrategy.WebSearch);
        var step = instance.Steps[0];
        
        // TimeZone: US/Eastern (UTC-5)
        // System.TimeZoneInfo id for Eastern depends on OS. Windows: "Eastern Standard Time", Linux/Mac: "America/New_York"
        // Let's try finding one that works or use "UTC" offset if generic.
        // Ideally we use "America/New_York" for cross-platform modern .NET (ICU).
        string tzId = "America/New_York";
        try { TimeZoneInfo.FindSystemTimeZoneById(tzId); }
        catch { tzId = "Eastern Standard Time"; } // Windows fallback
        
        var tz = TimeZoneInfo.FindSystemTimeZoneById(tzId);
        
        // Start: 2023-01-01 12:00:00 UTC
        // In EST (UTC-5): 2023-01-01 07:00:00 AM
        var startUtc = new DateTime(2023, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        
        // Act
        instance.Activate(startUtc, tzId);
        
        // Assert
        // Target: Day 0 (2023-01-01) at 09:00:00 EST
        // 09:00 EST = 14:00 UTC
        var expectedUtc = new DateTime(2023, 1, 1, 14, 0, 0, DateTimeKind.Utc);
        
        Assert.Equal(expectedUtc, step.RunAt);
        Assert.Equal(tzId, instance.TimeZoneId);
    }

    [Fact]
    public void Schedule_HandlesDayOffset_Pacific()
    {
        // Assemble
        var instance = WorkflowInstance.Create(Guid.NewGuid());
        // Step: Day 1, 10:00 AM
        instance.AddStep(WorkflowStepType.Email, 1, new TimeSpan(10, 0, 0), ContentGenerationStrategy.WebSearch);
        var step = instance.Steps[0];
        
        string tzId = "America/Los_Angeles";
        try { TimeZoneInfo.FindSystemTimeZoneById(tzId); }
        catch { tzId = "Pacific Standard Time"; }
        
        // Start: 2023-01-01 22:00:00 UTC
        // In PST (UTC-8): 2023-01-01 14:00:00 (2pm)
        var startUtc = new DateTime(2023, 1, 1, 22, 0, 0, DateTimeKind.Utc);
        
        // Act
        instance.Activate(startUtc, tzId);
        
        // Assert
        // Target: Day 1 (Relative to Start Day 2023-01-01) -> 2023-01-02
        // Time: 10:00 AM PST
        // 2023-01-02 10:00:00 PST = 18:00:00 UTC
        var expectedUtc = new DateTime(2023, 1, 2, 18, 0, 0, DateTimeKind.Utc);
        
        Assert.Equal(expectedUtc, step.RunAt);
    }
}
