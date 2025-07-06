using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Tasker.Infrastructure.Data;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<TaskerDbContext>
{
    public TaskerDbContext CreateDbContext(string[] args)
    {
        // Default to SQLite for design-time (migrations, etc.)
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
            "Tasker");
        
        if (!Directory.Exists(appDataPath))
            Directory.CreateDirectory(appDataPath);
            
        var dbPath = Path.Combine(appDataPath, "tasker.db");
        var connectionString = $"Data Source={dbPath}";
        
        var optionsBuilder = new DbContextOptionsBuilder<TaskerDbContext>();
        optionsBuilder.UseSqlite(connectionString);

        return new TaskerDbContext(optionsBuilder.Options);
    }
}