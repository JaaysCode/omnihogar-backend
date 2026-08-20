namespace OmniHogar.Domain.Common;

/// <summary>
/// Base class for all domain entities. Provides identity and audit fields.
/// </summary>
public abstract class BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
}
