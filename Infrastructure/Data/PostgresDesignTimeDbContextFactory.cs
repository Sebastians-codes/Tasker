using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System.Text.Json;

namespace Tasker.Infrastructure.Data;

public class PostgresDesignTimeDbContextFactory : IDesignTimeDbContextFactory<PostgresDbContext>
{
    public PostgresDbContext CreateDbContext(string[] args)
    {
        var connectionString = GetConnectionString();
        
        var optionsBuilder = new DbContextOptionsBuilder<PostgresDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new PostgresDbContext(optionsBuilder.Options);
    }

    private static string GetConnectionString()
    {
        // Try to get connection string from config.json first
        var configPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
            "Tasker", 
            "config.json");

        if (File.Exists(configPath))
        {
            try
            {
                var json = File.ReadAllText(configPath);
                var config = JsonSerializer.Deserialize<JsonElement>(json);
                
                if (config.TryGetProperty("EncryptedConnectionString", out var encryptedConnectionString))
                {
                    var connectionStringValue = encryptedConnectionString.GetString();
                    if (!string.IsNullOrEmpty(connectionStringValue))
                    {
                        // For design-time, we'll use the fallback connection string
                        // since we can't decrypt without the full CLI context
                        return "Host=localhost;Database=tasker;Username=postgres;Password=password";
                    }
                }
            }
            catch
            {
                // If config reading fails, use fallback
            }
        }
        
        // Fallback PostgreSQL connection string for design-time operations
        return "Host=localhost;Database=tasker;Username=postgres;Password=password";
    }
}