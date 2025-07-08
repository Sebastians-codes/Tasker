using Microsoft.EntityFrameworkCore;
using Tasker.Domain.Models;

namespace Tasker.Infrastructure.Data;

public class SyncService
{
    private readonly PostgresDbContext _postgresContext;
    private readonly SqliteDbContext _sqliteContext;
    private readonly IConnectionMonitor _connectionMonitor;

    public SyncService(
        PostgresDbContext postgresContext, 
        SqliteDbContext sqliteContext,
        IConnectionMonitor connectionMonitor)
    {
        _postgresContext = postgresContext;
        _sqliteContext = sqliteContext;
        _connectionMonitor = connectionMonitor;
        
        // Subscribe to connection status changes
        _connectionMonitor.ConnectionStatusChanged += OnConnectionStatusChanged;
    }

    private async void OnConnectionStatusChanged(object? sender, bool isConnected)
    {
        if (isConnected)
        {
            // PostgreSQL is back online, sync unsynced data
            await SyncToPostgresAsync();
        }
    }

    public async Task SyncToPostgresAsync()
    {
        if (!await _connectionMonitor.IsPostgresAvailableAsync())
        {
            return;
        }

        try
        {
            // Check if PostgreSQL is empty and reset sync status if needed
            await CheckAndResetSyncStatusAsync();
            
            // Sync in dependency order: Users first, then Projects, then Tasks
            // UserSessions are machine-specific and should not be synced
            await SyncEntities<User>();
            await SyncEntities<Project>();
            await SyncEntities<Tasks>();
        }
        catch (Exception ex)
        {
            // Log error but don't throw - sync will retry later
            Console.WriteLine($"Sync failed: {ex.Message}");
        }
    }

    private async Task CheckAndResetSyncStatusAsync()
    {
        // Check if PostgreSQL is empty (no users, projects, or tasks)
        var userCount = await _postgresContext.Users.CountAsync(x => !x.IsDeleted);
        var projectCount = await _postgresContext.Projects.CountAsync(x => !x.IsDeleted);
        var taskCount = await _postgresContext.Tasks.CountAsync(x => !x.IsDeleted);
        
        if (userCount == 0 && projectCount == 0 && taskCount == 0)
        {
            // Reset sync status for all entities in SQLite
            await ResetSyncStatus<User>();
            await ResetSyncStatus<Project>();
            await ResetSyncStatus<Tasks>();
        }
    }

    private async Task ResetSyncStatus<T>() where T : BaseEntity
    {
        var entities = await _sqliteContext.Set<T>()
            .Where(x => x.IsSynced)
            .ToListAsync();
        
        foreach (var entity in entities)
        {
            entity.IsSynced = false;
        }
        
        if (entities.Any())
        {
            await _sqliteContext.SaveChangesAsync();
        }
    }

    private async Task SyncEntities<T>() where T : BaseEntity
    {
        var unsyncedEntities = await _sqliteContext.Set<T>()
            .Where(x => !x.IsSynced)
            .ToListAsync();

        foreach (var entity in unsyncedEntities)
        {
            try
            {
                await SyncEntity(entity);
                
                // Mark as synced in SQLite
                entity.IsSynced = true;
                await _sqliteContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to sync {typeof(T).Name} with ID {entity.Id}: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
                // Skip this entity and continue with others
                continue;
            }
        }
    }

    private async Task SyncEntity<T>(T entity) where T : BaseEntity
    {
        // Clear change trackers to avoid tracking conflicts
        _postgresContext.ChangeTracker.Clear();
        _sqliteContext.ChangeTracker.Clear();

        var existingEntity = await _postgresContext.Set<T>()
            .FirstOrDefaultAsync(x => x.Id == entity.Id);

        if (existingEntity == null)
        {
            // Entity doesn't exist in PostgreSQL, add it (including deleted ones)
            var newEntity = CloneEntityForPostgres(entity);
            newEntity.IsSynced = true;
            _postgresContext.Set<T>().Add(newEntity);
        }
        else
        {
            // Entity exists, check for conflicts
            if (existingEntity.LastModified > entity.LastModified)
            {
                // PostgreSQL has newer version, update SQLite
                var updatedEntity = CloneEntityForSqlite(existingEntity);
                updatedEntity.IsSynced = true;
                _sqliteContext.Set<T>().Update(updatedEntity);
                await _sqliteContext.SaveChangesAsync();
                return;
            }
            else
            {
                // SQLite has newer version, update PostgreSQL
                CopyEntityProperties(entity, existingEntity);
                existingEntity.IsSynced = true;
            }
        }

        await _postgresContext.SaveChangesAsync();
    }

    private T CloneEntityForPostgres<T>(T entity) where T : BaseEntity
    {
        // Create a clean instance with only the essential properties (no navigation properties)
        var clonedEntity = Activator.CreateInstance<T>();
        
        // Copy all simple properties (not navigation properties)
        var properties = typeof(T).GetProperties()
            .Where(p => p.CanWrite && p.CanRead && !IsNavigationProperty(p));
        
        foreach (var property in properties)
        {
            var value = property.GetValue(entity);
            
            // Convert DateTime values to UTC for PostgreSQL
            if (value is DateTime dt)
            {
                if (dt.Kind == DateTimeKind.Unspecified)
                {
                    value = DateTime.SpecifyKind(dt, DateTimeKind.Utc);
                }
            }
            else if (property.PropertyType == typeof(DateTime?) && value != null)
            {
                var nullableDateTime = (DateTime?)value;
                if (nullableDateTime.HasValue && nullableDateTime.Value.Kind == DateTimeKind.Unspecified)
                {
                    value = DateTime.SpecifyKind(nullableDateTime.Value, DateTimeKind.Utc);
                }
            }
            
            property.SetValue(clonedEntity, value);
        }
        
        return clonedEntity;
    }

    private T CloneEntityForSqlite<T>(T entity) where T : BaseEntity
    {
        // Create a clean instance with only the essential properties (no navigation properties)
        var clonedEntity = Activator.CreateInstance<T>();
        
        // Copy all simple properties (not navigation properties)
        var properties = typeof(T).GetProperties()
            .Where(p => p.CanWrite && p.CanRead && !IsNavigationProperty(p));
        
        foreach (var property in properties)
        {
            var value = property.GetValue(entity);
            property.SetValue(clonedEntity, value);
        }
        
        return clonedEntity;
    }

    private bool IsNavigationProperty(System.Reflection.PropertyInfo property)
    {
        // Skip navigation properties that would cause tracking conflicts
        var navigationPropertyNames = new[] { "User", "Project", "Owner", "Tasks", "Projects" };
        return navigationPropertyNames.Contains(property.Name);
    }

    private void CopyEntityProperties<T>(T source, T destination) where T : BaseEntity
    {
        var properties = typeof(T).GetProperties()
            .Where(p => p.CanWrite && p.Name != "Id");

        foreach (var property in properties)
        {
            var value = property.GetValue(source);
            
            // Convert DateTime values to UTC for PostgreSQL
            if (value is DateTime dt)
            {
                if (dt.Kind == DateTimeKind.Unspecified)
                {
                    value = DateTime.SpecifyKind(dt, DateTimeKind.Utc);
                }
            }
            else if (property.PropertyType == typeof(DateTime?) && value != null)
            {
                var nullableDateTime = (DateTime?)value;
                if (nullableDateTime.HasValue && nullableDateTime.Value.Kind == DateTimeKind.Unspecified)
                {
                    value = DateTime.SpecifyKind(nullableDateTime.Value, DateTimeKind.Utc);
                }
            }
            
            property.SetValue(destination, value);
        }
    }

    public async Task FullSyncFromPostgresAsync()
    {
        if (!await _connectionMonitor.IsPostgresAvailableAsync())
            return;

        try
        {
            // Sync from PostgreSQL: Users first, then Projects, then Tasks
            // UserSessions are machine-specific and should not be synced
            await SyncFromPostgres<User>();
            await SyncFromPostgres<Project>();
            await SyncFromPostgres<Tasks>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Full sync failed: {ex.Message}");
        }
    }

    private async Task SyncFromPostgres<T>() where T : BaseEntity
    {
        var postgresEntities = await _postgresContext.Set<T>()
            .Where(x => !x.IsDeleted)
            .ToListAsync();

        foreach (var entity in postgresEntities)
        {
            var sqliteEntity = await _sqliteContext.Set<T>()
                .FirstOrDefaultAsync(x => x.Id == entity.Id);

            if (sqliteEntity == null)
            {
                // Entity doesn't exist in SQLite, add it
                var newEntity = CloneEntityForSqlite(entity);
                newEntity.IsSynced = true;
                _sqliteContext.Set<T>().Add(newEntity);
            }
            else if (entity.LastModified > sqliteEntity.LastModified)
            {
                // PostgreSQL has newer version, update SQLite
                CopyEntityProperties(entity, sqliteEntity);
                sqliteEntity.IsSynced = true;
            }
        }

        await _sqliteContext.SaveChangesAsync();
    }
}