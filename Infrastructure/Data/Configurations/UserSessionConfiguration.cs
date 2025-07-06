using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tasker.Domain.Models;

namespace Tasker.Infrastructure.Data.Configurations;

public class UserSessionConfiguration : IEntityTypeConfiguration<UserSession>
{
    public void Configure(EntityTypeBuilder<UserSession> builder)
    {
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.Token)
            .HasMaxLength(256)
            .IsRequired();
            
        builder.Property(x => x.ExpiresAt)
            .IsRequired();
            
        builder.Property(x => x.CreatedAt)
            .IsRequired();
            
        builder.Property(x => x.DurationDays)
            .IsRequired();
            
        builder.Property(x => x.AutoLoginEnabled)
            .IsRequired();
            
        builder.Property(x => x.MachineId)
            .HasMaxLength(128)
            .IsRequired();

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.HasIndex(x => x.Token)
            .IsUnique();
    }
}