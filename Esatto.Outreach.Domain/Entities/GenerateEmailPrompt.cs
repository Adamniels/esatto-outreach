namespace Esatto.Outreach.Domain.Entities;


public class GenerateEmailPrompt
{
    public Guid Id { get; private set; }
    public string Instructions { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public DateTime CreatedUtc { get; private set; }
    public DateTime UpdatedUtc { get; private set; }

    protected GenerateEmailPrompt() { }

    public static GenerateEmailPrompt Create(string instructions, bool isActive = false)
    {
        if (string.IsNullOrWhiteSpace(instructions))
            throw new ArgumentException("Instructions cannot be empty", nameof(instructions));

        return new GenerateEmailPrompt
        {
            Id = Guid.NewGuid(),
            Instructions = instructions,
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

