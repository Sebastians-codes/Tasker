using Tasker.Domain.Services;

namespace Domain.Models;

public class Project : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Priority Priority { get; set; }
    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedOn { get; set; }
    public DateTime CompletedOn { get; set; }

    public Guid OwnerId { get; set; }
    public User Owner { get; set; } = null!;

    public List<Tasks> Tasks = [];

    public void Encrypt()
    {
        Name = DomainEncryptionService.Encrypt(Name, OwnerId);
        Description = DomainEncryptionService.Encrypt(Description, OwnerId);
    }

    public void Decrypt()
    {
        Name = DomainEncryptionService.Decrypt(Name, OwnerId);
        Description = DomainEncryptionService.Decrypt(Description, OwnerId);
    }
}
