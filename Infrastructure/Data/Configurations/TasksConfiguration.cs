using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Tasker.Infrastructure.Data.Configurations;

public class TasksConfiguration : IEntityTypeConfiguration<Tasks>
{
    public void Configure(EntityTypeBuilder<Tasks> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Title)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(t => t.Description)
            .HasMaxLength(1000);

        builder.Property(t => t.Priority)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(t => t.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(t => t.ActualTimeMinutes)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(t => t.ActiveStartTime);
        builder.Property(t => t.LastPausedTime);

        builder.Property(t => t.CreatedOn)
            .IsRequired();

        builder.HasOne(t => t.Project)
            .WithMany(p => p.Tasks)
            .HasForeignKey(t => t.ProjectId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(t => t.User)
            .WithMany(u => u.Tasks)
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(t => new { t.Title, t.ProjectId, t.UserId })
            .IsUnique();
        builder.HasIndex(t => t.ProjectId);
        builder.HasIndex(t => t.UserId);
        builder.HasIndex(t => t.Priority);
        builder.HasIndex(t => t.Status);
        builder.HasIndex(t => t.CreatedOn);
        builder.HasIndex(t => t.DueDate);

        // Configure BaseEntity properties
        builder.Property(t => t.LastModified)
            .IsRequired();

        builder.Property(t => t.IsSynced)
            .IsRequired();

        builder.Property(t => t.IsDeleted)
            .IsRequired();
    }
}
