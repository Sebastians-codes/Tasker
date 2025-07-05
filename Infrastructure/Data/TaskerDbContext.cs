using Microsoft.EntityFrameworkCore;
using Tasker.Domain.Models;

namespace Tasker.Infrastructure.Data;

public class TaskerDbContext(DbContextOptions<TaskerDbContext> options) : DbContext(options)
{
    public DbSet<Tasks> Tasks { get; set; } = null!;
    public DbSet<Project> Projects { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TaskerDbContext).Assembly);
    }
}
