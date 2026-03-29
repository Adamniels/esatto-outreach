using Esatto.Outreach.Domain.Enums;

namespace Esatto.Outreach.Domain.Extensions;

public static class WorkflowStepTypeExtensions
{
    public static bool RequiresContent(this WorkflowStepType type) =>
        type is WorkflowStepType.Email or WorkflowStepType.LinkedInMessage;

    public static void ValidateGenerationStrategy(this WorkflowStepType type, ContentGenerationStrategy? strategy)
    {
        if (type.RequiresContent() && strategy == null)
            throw new ArgumentException($"{type} step requires a content generation strategy");

        if (!type.RequiresContent() && strategy != null)
            throw new ArgumentException($"{type} step should not have a content generation strategy");
    }
}
