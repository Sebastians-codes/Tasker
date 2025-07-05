namespace Tasker.Domain.Models;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public int FailedLoginAttempts { get; set; } = 0;
    public DateTime? LockoutEndTime { get; set; }

    public List<Tasks> Tasks { get; set; } = [];
    public List<Project> Projects { get; set; } = [];
}