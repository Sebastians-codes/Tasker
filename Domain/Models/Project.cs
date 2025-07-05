namespace Tasker.Domain.Models;

public class Project
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Priority Priority { get; set; }
    public DateTimeOffset CreatedOn { get; set; } = DateTimeOffset.Now;
    public DateTimeOffset UpdatedOn { get; set; }
    public DateTimeOffset CompletedOn { get; set; }
    public DateTimeOffset? DueDate { get; set; }

    public List<Tasks> Tasks = [];
}
