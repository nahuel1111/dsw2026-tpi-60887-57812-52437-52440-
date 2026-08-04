namespace Dsw2026Tpi.Domain.Entities;

public abstract class EntityBase(Guid? id = null)
{
    public Guid Id { get; init; } = id ?? Guid.NewGuid();

    public DateTime CreatedAt { get; protected set; } = DateTime.Now;
    public DateTime UpdatedAt { get; protected set; } = DateTime.Now;
}
