using Microsoft.EntityFrameworkCore;
using Tasker.Domain.Models;
using System.Text.Json;

namespace Tasker.Infrastructure.Data;

public class DatabaseManager
{
    private readonly PostgresDbContext _postgresContext;
    private readonly SqliteDbContext _sqliteContext;
    private readonly IConnectionMonitor _connectionMonitor;

    public DatabaseManager(
        PostgresDbContext postgresContext, 
        SqliteDbContext sqliteContext,
        IConnectionMonitor connectionMonitor)
    {
        _postgresContext = postgresContext;
        _sqliteContext = sqliteContext;
        _connectionMonitor = connectionMonitor;
    }

    public async Task<T?> GetAsync<T>(int id) where T : BaseEntity
    {
        T? result = null;
        
        if (await _connectionMonitor.IsPostgresAvailableAsync())
        {
            try
            {
                result = await _postgresContext.Set<T>()
                    .Where(x => !x.IsDeleted)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == id);
            }
            catch
            {
                // Fall back to SQLite if PostgreSQL fails
            }
        }

        if (result == null)
        {
            result = await _sqliteContext.Set<T>()
                .Where(x => !x.IsDeleted)
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        return result;
    }

    public async Task<List<T>> GetAllAsync<T>() where T : BaseEntity
    {
        if (await _connectionMonitor.IsPostgresAvailableAsync())
        {
            try
            {
                return await _postgresContext.Set<T>()
                    .Where(x => !x.IsDeleted)
                    .AsNoTracking()
                    .ToListAsync();
            }
            catch
            {
                // Fall back to SQLite if PostgreSQL fails
            }
        }

        return await _sqliteContext.Set<T>()
            .Where(x => !x.IsDeleted)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<List<Tasks>> GetAllTasksWithProjectAsync()
    {
        if (await _connectionMonitor.IsPostgresAvailableAsync())
        {
            try
            {
                return await _postgresContext.Tasks
                    .Include(t => t.Project)
                    .Where(x => !x.IsDeleted)
                    .AsNoTracking()
                    .ToListAsync();
            }
            catch
            {
                // Fall back to SQLite if PostgreSQL fails
            }
        }

        return await _sqliteContext.Tasks
            .Include(t => t.Project)
            .Where(x => !x.IsDeleted)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<Tasks?> GetTaskWithProjectAsync(int id)
    {
        if (await _connectionMonitor.IsPostgresAvailableAsync())
        {
            try
            {
                return await _postgresContext.Tasks
                    .Include(t => t.Project)
                    .Where(x => !x.IsDeleted && x.Id == id)
                    .AsNoTracking()
                    .FirstOrDefaultAsync();
            }
            catch
            {
                // Fall back to SQLite if PostgreSQL fails
            }
        }

        return await _sqliteContext.Tasks
            .Include(t => t.Project)
            .Where(x => !x.IsDeleted && x.Id == id)
            .AsNoTracking()
            .FirstOrDefaultAsync();
    }

    public async Task<List<Project>> GetAllProjectsWithTasksAsync()
    {
        if (await _connectionMonitor.IsPostgresAvailableAsync())
        {
            try
            {
                return await _postgresContext.Projects
                    .Include(p => p.Tasks)
                    .Where(x => !x.IsDeleted)
                    .AsNoTracking()
                    .ToListAsync();
            }
            catch
            {
                // Fall back to SQLite if PostgreSQL fails
            }
        }

        return await _sqliteContext.Projects
            .Include(p => p.Tasks)
            .Where(x => !x.IsDeleted)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<Project?> GetProjectWithTasksAsync(int id)
    {
        if (await _connectionMonitor.IsPostgresAvailableAsync())
        {
            try
            {
                return await _postgresContext.Projects
                    .Include(p => p.Tasks)
                    .Where(x => !x.IsDeleted && x.Id == id)
                    .AsNoTracking()
                    .FirstOrDefaultAsync();
            }
            catch
            {
                // Fall back to SQLite if PostgreSQL fails
            }
        }

        return await _sqliteContext.Projects
            .Include(p => p.Tasks)
            .Where(x => !x.IsDeleted && x.Id == id)
            .AsNoTracking()
            .FirstOrDefaultAsync();
    }

    public async Task<T> AddAsync<T>(T entity) where T : BaseEntity
    {
        // Clear change tracker to avoid tracking conflicts
        _sqliteContext.ChangeTracker.Clear();
        _postgresContext.ChangeTracker.Clear();
        
        // Always add to SQLite first
        entity.LastModified = DateTime.UtcNow;
        entity.IsSynced = false;
        entity.SyncVersion = Guid.NewGuid().ToString();
        
        _sqliteContext.Set<T>().Add(entity);
        await _sqliteContext.SaveChangesAsync();

        // Try to add to PostgreSQL if available
        if (await _connectionMonitor.IsPostgresAvailableAsync())
        {
            try
            {
                var postgresEntity = CloneEntityForPostgres(entity);
                postgresEntity.IsSynced = true;
                
                _postgresContext.Set<T>().Add(postgresEntity);
                await _postgresContext.SaveChangesAsync();
                
                // Mark as synced in SQLite
                entity.IsSynced = true;
                await _sqliteContext.SaveChangesAsync();
            }
            catch
            {
                // PostgreSQL failed, data remains in SQLite as unsynced
            }
        }

        return entity;
    }

    public async Task<T> UpdateAsync<T>(T entity) where T : BaseEntity
    {
        // Clear change tracker to avoid tracking conflicts
        _sqliteContext.ChangeTracker.Clear();
        _postgresContext.ChangeTracker.Clear();
        
        // Always update SQLite first
        entity.LastModified = DateTime.UtcNow;
        entity.IsSynced = false;
        entity.SyncVersion = Guid.NewGuid().ToString();
        
        _sqliteContext.Set<T>().Update(entity);
        await _sqliteContext.SaveChangesAsync();

        // Try to update PostgreSQL if available
        if (await _connectionMonitor.IsPostgresAvailableAsync())
        {
            try
            {
                // Find the existing entity in PostgreSQL
                var existingEntity = await _postgresContext.Set<T>()
                    .Where(e => e.Id == entity.Id && !e.IsDeleted)
                    .FirstOrDefaultAsync();
                
                if (existingEntity != null)
                {
                    // Update properties of the existing tracked entity
                    UpdateEntityProperties(entity, existingEntity);
                    existingEntity.IsSynced = true;
                    
                    await _postgresContext.SaveChangesAsync();
                }
                else
                {
                    // Entity doesn't exist in PostgreSQL, add it
                    var postgresEntity = CloneEntityForPostgres(entity);
                    postgresEntity.IsSynced = true;
                    
                    _postgresContext.Set<T>().Add(postgresEntity);
                    await _postgresContext.SaveChangesAsync();
                }
                
                // Mark as synced in SQLite
                entity.IsSynced = true;
                await _sqliteContext.SaveChangesAsync();
            }
            catch
            {
                // PostgreSQL failed, data remains in SQLite as unsynced
            }
        }

        return entity;
    }

    public async Task DeleteAsync<T>(int id) where T : BaseEntity
    {
        // Soft delete in SQLite
        var sqliteEntity = await _sqliteContext.Set<T>().FindAsync(id);
        if (sqliteEntity != null)
        {
            sqliteEntity.IsDeleted = true;
            await _sqliteContext.SaveChangesAsync();
        }

        // Try to delete from PostgreSQL if available
        if (await _connectionMonitor.IsPostgresAvailableAsync())
        {
            try
            {
                var postgresEntity = await _postgresContext.Set<T>().FindAsync(id);
                if (postgresEntity != null)
                {
                    postgresEntity.IsDeleted = true;
                    await _postgresContext.SaveChangesAsync();
                    
                    // Mark as synced in SQLite
                    if (sqliteEntity != null)
                    {
                        sqliteEntity.IsSynced = true;
                        await _sqliteContext.SaveChangesAsync();
                    }
                }
            }
            catch
            {
                // PostgreSQL failed, deletion remains in SQLite as unsynced
            }
        }
    }

    public async Task<List<T>> GetUnsyncedAsync<T>() where T : BaseEntity
    {
        return await _sqliteContext.Set<T>()
            .Where(x => !x.IsSynced)
            .ToListAsync();
    }

    public async Task<User?> GetUserByUsernameAsync(string username)
    {
        if (await _connectionMonitor.IsPostgresAvailableAsync())
        {
            try
            {
                return await _postgresContext.Users
                    .Where(x => !x.IsDeleted && x.Username == username)
                    .AsNoTracking()
                    .FirstOrDefaultAsync();
            }
            catch
            {
                // Fall back to SQLite if PostgreSQL fails
            }
        }

        return await _sqliteContext.Users
            .Where(x => !x.IsDeleted && x.Username == username)
            .AsNoTracking()
            .FirstOrDefaultAsync();
    }

    public async Task<bool> UsernameExistsAsync(string username)
    {
        var user = await GetUserByUsernameAsync(username);
        return user != null;
    }

    public async Task<UserSession?> GetSessionByTokenAsync(string token)
    {
        if (await _connectionMonitor.IsPostgresAvailableAsync())
        {
            try
            {
                return await _postgresContext.UserSessions
                    .Include(x => x.User)
                    .Where(x => !x.IsDeleted && x.Token == token)
                    .AsNoTracking()
                    .FirstOrDefaultAsync();
            }
            catch
            {
                // Fall back to SQLite if PostgreSQL fails
            }
        }

        return await _sqliteContext.UserSessions
            .Include(x => x.User)
            .Where(x => !x.IsDeleted && x.Token == token)
            .AsNoTracking()
            .FirstOrDefaultAsync();
    }

    private void DetachEntityFromContexts<T>(T entity) where T : BaseEntity
    {
        // Detach from PostgreSQL context if tracked
        var postgresEntry = _postgresContext.Entry(entity);
        if (postgresEntry.State != EntityState.Detached)
        {
            postgresEntry.State = EntityState.Detached;
        }
        
        // Detach from SQLite context if tracked
        var sqliteEntry = _sqliteContext.Entry(entity);
        if (sqliteEntry.State != EntityState.Detached)
        {
            sqliteEntry.State = EntityState.Detached;
        }
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
        
        // Ensure sync metadata is set (also convert to UTC)
        clonedEntity.LastModified = entity.LastModified.Kind == DateTimeKind.Unspecified 
            ? DateTime.SpecifyKind(entity.LastModified, DateTimeKind.Utc) 
            : entity.LastModified;
        clonedEntity.SyncVersion = entity.SyncVersion;
        clonedEntity.IsSynced = true;
        
        return clonedEntity;
    }

    private bool IsNavigationProperty(System.Reflection.PropertyInfo property)
    {
        // Skip navigation properties that would cause tracking conflicts
        var navigationPropertyNames = new[] { "User", "Project", "Owner", "Tasks", "Projects" };
        return navigationPropertyNames.Contains(property.Name);
    }

    private void UpdateEntityProperties<T>(T source, T target) where T : BaseEntity
    {
        // Copy all simple properties (not navigation properties) from source to target
        var properties = typeof(T).GetProperties()
            .Where(p => p.CanWrite && p.CanRead && !IsNavigationProperty(p) && p.Name != "Id");
        
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
            
            property.SetValue(target, value);
        }
    }

    private T CloneEntity<T>(T entity) where T : BaseEntity
    {
        // Simple cloning for sync operations
        var json = System.Text.Json.JsonSerializer.Serialize(entity, new JsonSerializerOptions
        {
            ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles
        });
        return System.Text.Json.JsonSerializer.Deserialize<T>(json)!;
    }
}