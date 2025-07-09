using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tasker.Domain.Models;

namespace Tasker.Infrastructure.Data.Configurations;

public class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(p => p.Description)
            .HasMaxLength(500);

        builder.Property(p => p.Priority)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(p => p.CreatedOn)
            .IsRequired();

        builder.HasMany(p => p.Tasks)
            .WithOne(t => t.Project)
            .HasForeignKey(t => t.ProjectId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(p => new { p.Name, p.OwnerId })
            .IsUnique();
        builder.HasIndex(p => p.Priority);
        builder.HasIndex(p => p.CreatedOn);
        
        // Configure BaseEntity properties
        builder.Property(p => p.LastModified)
            .IsRequired();
            
        builder.Property(p => p.IsSynced)
            .IsRequired();
            
        builder.Property(p => p.IsDeleted)
            .IsRequired();
    }
}