using Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Tasker.Infrastructure.Data;

public class SqliteDbContext : DbContext
{
    public SqliteDbContext(DbContextOptions<SqliteDbContext> options) : base(options) { }

    public DbSet<Tasks> Tasks { get; set; } = null!;
    public DbSet<Project> Projects { get; set; } = null!;
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<UserSession> UserSessions { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SqliteDbContext).Assembly);
    }

}
