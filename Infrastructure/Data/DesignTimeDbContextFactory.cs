using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Tasker.Infrastructure.Data;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<TaskerDbContext>
{
    public TaskerDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<TaskerDbContext>();
        optionsBuilder.UseSqlite("Data Source=tasker.db");

        return new TaskerDbContext(optionsBuilder.Options);
    }
}