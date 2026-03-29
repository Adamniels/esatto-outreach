using Esatto.Outreach.Domain.Enums;

namespace Esatto.Outreach.Domain.Entities;


public class OutreachPrompt
{
    public Guid Id { get; private set; }
    public string Instructions { get; private set; } = string.Empty;
    public PromptType Type { get; private set; }
    public bool IsActive { get; private set; }
    public string UserId { get; private set; } = string.Empty;
    public DateTime CreatedUtc { get; private set; }
    public DateTime UpdatedUtc { get; private set; }

    // Navigation property
    public ApplicationUser User { get; private set; } = null!;

    protected OutreachPrompt() { }

    public static OutreachPrompt Create(string userId, string instructions, PromptType type, bool isActive = false)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("UserId cannot be empty", nameof(userId));
            
        if (string.IsNullOrWhiteSpace(instructions))
            throw new ArgumentException("Instructions cannot be empty", nameof(instructions));

        return new OutreachPrompt
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Instructions = instructions,
            Type = type,
            IsActive = isActive,
            CreatedUtc = DateTime.UtcNow,
            UpdatedUtc = DateTime.UtcNow
        };
    }

    public void UpdateInstructions(string instructions)
    {
        if (string.IsNullOrWhiteSpace(instructions))
            throw new ArgumentException("Instructions cannot be empty", nameof(instructions));

        Instructions = instructions;
        UpdatedUtc = DateTime.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedUtc = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedUtc = DateTime.UtcNow;
    }

}

