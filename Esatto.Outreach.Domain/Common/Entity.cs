namespace Esatto.Outreach.Domain.Common;

public abstract class Entity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
    public DateTime CreatedUtc { get; protected set; } = DateTime.UtcNow;
    public DateTime? UpdatedUtc { get; protected set; }

    public void Touch() => UpdatedUtc = DateTime.UtcNow;
}
