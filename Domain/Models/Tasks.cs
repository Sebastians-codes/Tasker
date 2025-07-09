using Tasker.Domain.Services;

namespace Tasker.Domain.Models;

public class Tasks : BaseEntity
{
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

    public Guid UserId { get; set; }
    public User User { get; set; }

    public Guid? ProjectId { get; set; }
    public Project? Project { get; set; }

    public void Encrypt()
    {
        Title = DomainEncryptionService.Encrypt(Title, UserId);
        Description = DomainEncryptionService.Encrypt(Description, UserId);
        if (!string.IsNullOrEmpty(AssignedTo))
            AssignedTo = DomainEncryptionService.Encrypt(AssignedTo, UserId);
    }

    public void Decrypt()
    {
        Title = DomainEncryptionService.Decrypt(Title, UserId);
        Description = DomainEncryptionService.Decrypt(Description, UserId);
        if (!string.IsNullOrEmpty(AssignedTo))
            AssignedTo = DomainEncryptionService.Decrypt(AssignedTo, UserId);
    }
}