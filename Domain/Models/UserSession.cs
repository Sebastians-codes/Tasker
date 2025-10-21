namespace Domain.Models;

public class UserSession : BaseEntity
{
    public Guid UserId { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int DurationDays { get; set; }
    public bool AutoLoginEnabled { get; set; }
    public string MachineId { get; set; } = string.Empty;

    public User User { get; set; } = null!;
}
