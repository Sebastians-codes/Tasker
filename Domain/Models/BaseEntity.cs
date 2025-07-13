namespace Tasker.Domain.Models;

public abstract class BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime LastModified { get; set; } = DateTime.UtcNow;
    public bool IsSynced { get; set; } = false;
    public string? SyncVersion { get; set; }
    public bool IsDeleted { get; set; } = false;
}