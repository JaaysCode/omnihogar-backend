namespace OmniHogar.Domain.Entities;

/// <summary>Generic action trail across entities (audit_log).</summary>
public class AuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid? UserId { get; set; }
    public User? User { get; set; }

    public string Action { get; set; } = string.Empty;
    public string Entity { get; set; } = string.Empty;

    /// <summary>Id of the referenced row in the table named by <see cref="Entity"/>.</summary>
    public Guid? EntityId { get; set; }

    public string? Detail { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
