namespace Tasker.Domain.Models;

public class Tasks
{
    public int Id { get; init; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Priority Priority { get; set; } = Priority.None;
    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedOn { get; set; }
    public DateTime? CompletedOn { get; set; }
    public DateTime? DueDate { get; set; }
    public string? AssignedTo { get; set; }
    public int? TimeEstimateMinutes { get; set; }
    public WorkStatus Status { get; set; } = WorkStatus.NotAssigned;

    public int ActualTimeMinutes { get; set; } = 0;
    public DateTime? ActiveStartTime { get; set; }
    public DateTime? LastPausedTime { get; set; }

    public int UserId { get; set; }
    public User User { get; set; }

    public int? ProjectId { get; set; }
    public Project? Project { get; set; }
}