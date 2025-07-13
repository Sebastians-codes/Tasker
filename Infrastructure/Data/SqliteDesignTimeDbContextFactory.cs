using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Tasker.Infrastructure.Data;

public class SqliteDesignTimeDbContextFactory : IDesignTimeDbContextFactory<SqliteDbContext>
{
    public SqliteDbContext CreateDbContext(string[] args)
    {
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
            "Tasker");
        
        if (!Directory.Exists(appDataPath))
            Directory.CreateDirectory(appDataPath);
            
        var dbPath = Path.Combine(appDataPath, "tasker_local.db");
        var connectionString = $"Data Source={dbPath}";
        
        var optionsBuilder = new DbContextOptionsBuilder<SqliteDbContext>();
        optionsBuilder.UseSqlite(connectionString);

        return new SqliteDbContext(optionsBuilder.Options);
    }
}