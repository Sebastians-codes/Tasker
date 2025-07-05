namespace Tasker.Domain.Models;

public class Tasks
{
    public int Id { get; init; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Priority Priority { get; set; } = Priority.None;
    public DateTimeOffset CreatedOn { get; set; } = DateTimeOffset.Now;
    public DateTimeOffset UpdatedOn { get; set; }
    public DateTimeOffset CompletedOn { get; set; }
    public DateTimeOffset? DueDate { get; set; }
    public string? AssignedTo { get; set; }
    public int? TimeEstimateMinutes { get; set; }
    public WorkStatus Status { get; set; } = WorkStatus.NotAssigned;

    public int ActualTimeMinutes { get; set; } = 0;
    public DateTimeOffset? ActiveStartTime { get; set; }
    public DateTimeOffset? LastPausedTime { get; set; }

    public int UserId { get; set; }
    public User User { get; set; }

    public int? ProjectId { get; set; }
    public Project? Project { get; set; }
}