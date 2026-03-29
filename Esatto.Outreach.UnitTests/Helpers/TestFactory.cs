using Esatto.Outreach.Domain.Entities;
using Esatto.Outreach.Domain.Enums;
using System.Reflection;

namespace Esatto.Outreach.UnitTests.Helpers;

public static class TestFactory
{
    public static Prospect CreateValidManualProspect(string name = "John Doe", string ownerId = "owner-1")
    {
        return Prospect.CreateManual(name, ownerId, new List<string> { "https://example.com" }, "Test notes");
    }

    public static Prospect CreateValidPendingCrmProspect(string externalId = "ext-1", string name = "Jane Doe", CrmProvider provider = CrmProvider.Capsule)
    {
        return Prospect.CreatePendingFromCrm(
            provider,
            externalId,
            name,
            "About Jane",
            DateTime.UtcNow,
            DateTime.UtcNow,
            null,
            null);
    }
    
    public static WorkflowInstance CreateWorkflowInstance(Guid? prospectId = null)
    {
        return WorkflowInstance.Create(prospectId ?? Guid.NewGuid());
    }

    public static void SetId<T>(T entity, Guid id) where T : class
    {
        var propertyInfo = typeof(T).GetProperty("Id", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (propertyInfo != null && propertyInfo.CanWrite)
        {
            propertyInfo.SetValue(entity, id, null);
        }
    }
}
