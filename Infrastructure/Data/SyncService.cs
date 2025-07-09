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
            // PostgreSQL is back online, sync unsynced data (all users)
            await SyncToPostgresAsync();
        }
    }

    public async Task SyncToPostgresAsync(Guid? userId = null)
    {
        if (!await _connectionMonitor.IsPostgresAvailableAsync())
        {
            return;
        }

        try
        {
            // Check if PostgreSQL is empty and reset sync status if needed
            await CheckAndResetSyncStatusAsync();
            
            // Sync in dependency order: Users first, then Projects, then Tasks, then UserSessions
            await SyncUsers(userId);
            await SyncProjects(userId);
            await SyncTasks(userId);
            await SyncUserSessions(userId);
        }
        catch (Exception ex)
        {
            // Log error but don't throw - sync will retry later
            // Silently fail - sync will retry later
        }
    }

    private async Task CheckAndResetSyncStatusAsync()
    {
        // Check if PostgreSQL is empty (no users, projects, tasks, or user sessions)
        var userCount = await _postgresContext.Users.CountAsync(x => !x.IsDeleted);
        var projectCount = await _postgresContext.Projects.CountAsync(x => !x.IsDeleted);
        var taskCount = await _postgresContext.Tasks.CountAsync(x => !x.IsDeleted);
        var sessionCount = await _postgresContext.UserSessions.CountAsync(x => !x.IsDeleted);
        
        if (userCount == 0 && projectCount == 0 && taskCount == 0 && sessionCount == 0)
        {
            // Reset sync status for all entities in SQLite
            await ResetSyncStatus<User>();
            await ResetSyncStatus<Project>();
            await ResetSyncStatus<Tasks>();
            await ResetSyncStatus<UserSession>();
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
        var navigationPropertyNames = new[] { "User", "Project", "Owner", "Tasks", "Projects", "UserSessions" };
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

    public async Task FullSyncFromPostgresAsync(Guid? userId = null)
    {
        if (!await _connectionMonitor.IsPostgresAvailableAsync())
            return;

        try
        {
            // Sync from PostgreSQL: Users first, then Projects, then Tasks, then UserSessions
            await SyncUsersFromPostgres(userId);
            await SyncProjectsFromPostgres(userId);
            await SyncTasksFromPostgres(userId);
            await SyncUserSessionsFromPostgres(userId);
        }
        catch (Exception ex)
        {
            // Silently fail - sync will retry later
        }
    }


    private async Task SyncUsers(Guid? userId)
    {
        var query = _sqliteContext.Users.Where(x => !x.IsSynced);
        if (userId.HasValue)
            query = query.Where(x => x.Id == userId.Value);
        
        var users = await query.ToListAsync();
        foreach (var user in users)
        {
            try
            {
                await SyncEntity(user);
                user.IsSynced = true;
                await _sqliteContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to sync User with ID {user.Id}: {ex.Message}");
                // Clear change trackers and continue with next entity
                _postgresContext.ChangeTracker.Clear();
                _sqliteContext.ChangeTracker.Clear();
                continue;
            }
        }
    }

    private async Task SyncProjects(Guid? userId)
    {
        var query = _sqliteContext.Projects.Where(x => !x.IsSynced);
        if (userId.HasValue)
            query = query.Where(x => x.OwnerId == userId.Value);
        
        var projects = await query.ToListAsync();
        foreach (var project in projects)
        {
            try
            {
                await SyncEntity(project);
                project.IsSynced = true;
                await _sqliteContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to sync Project with ID {project.Id}: {ex.Message}");
                // Clear change trackers and continue with next entity
                _postgresContext.ChangeTracker.Clear();
                _sqliteContext.ChangeTracker.Clear();
                continue;
            }
        }
    }

    private async Task SyncTasks(Guid? userId)
    {
        var query = _sqliteContext.Tasks.Where(x => !x.IsSynced);
        if (userId.HasValue)
            query = query.Where(x => x.UserId == userId.Value);
        
        var tasks = await query.ToListAsync();
        foreach (var task in tasks)
        {
            await SyncEntity(task);
            task.IsSynced = true;
            await _sqliteContext.SaveChangesAsync();
        }
    }

    private async Task SyncUserSessions(Guid? userId)
    {
        var query = _sqliteContext.UserSessions.Where(x => !x.IsSynced);
        if (userId.HasValue)
            query = query.Where(x => x.UserId == userId.Value);
        
        var sessions = await query.ToListAsync();
        foreach (var session in sessions)
        {
            await SyncEntity(session);
            session.IsSynced = true;
            await _sqliteContext.SaveChangesAsync();
        }
    }

    private async Task SyncUsersFromPostgres(Guid? userId)
    {
        var query = _postgresContext.Users.Where(x => !x.IsDeleted);
        if (userId.HasValue)
            query = query.Where(x => x.Id == userId.Value);
        
        var users = await query.ToListAsync();
        foreach (var user in users)
        {
            var sqliteUser = await _sqliteContext.Users.FirstOrDefaultAsync(x => x.Id == user.Id);
            if (sqliteUser == null)
            {
                var newUser = CloneEntityForSqlite(user);
                newUser.IsSynced = true;
                _sqliteContext.Users.Add(newUser);
            }
            else if (user.LastModified > sqliteUser.LastModified)
            {
                CopyEntityProperties(user, sqliteUser);
                sqliteUser.IsSynced = true;
            }
        }
        await _sqliteContext.SaveChangesAsync();
    }

    private async Task SyncProjectsFromPostgres(Guid? userId)
    {
        var query = _postgresContext.Projects.Where(x => !x.IsDeleted);
        if (userId.HasValue)
            query = query.Where(x => x.OwnerId == userId.Value);
        
        var projects = await query.ToListAsync();
        foreach (var project in projects)
        {
            var sqliteProject = await _sqliteContext.Projects.FirstOrDefaultAsync(x => x.Id == project.Id);
            if (sqliteProject == null)
            {
                var newProject = CloneEntityForSqlite(project);
                newProject.IsSynced = true;
                _sqliteContext.Projects.Add(newProject);
            }
            else if (project.LastModified > sqliteProject.LastModified)
            {
                CopyEntityProperties(project, sqliteProject);
                sqliteProject.IsSynced = true;
            }
        }
        await _sqliteContext.SaveChangesAsync();
    }

    private async Task SyncTasksFromPostgres(Guid? userId)
    {
        var query = _postgresContext.Tasks.Where(x => !x.IsDeleted);
        if (userId.HasValue)
            query = query.Where(x => x.UserId == userId.Value);
        
        var tasks = await query.ToListAsync();
        foreach (var task in tasks)
        {
            var sqliteTask = await _sqliteContext.Tasks.FirstOrDefaultAsync(x => x.Id == task.Id);
            if (sqliteTask == null)
            {
                var newTask = CloneEntityForSqlite(task);
                newTask.IsSynced = true;
                _sqliteContext.Tasks.Add(newTask);
            }
            else if (task.LastModified > sqliteTask.LastModified)
            {
                CopyEntityProperties(task, sqliteTask);
                sqliteTask.IsSynced = true;
            }
        }
        await _sqliteContext.SaveChangesAsync();
    }

    private async Task SyncUserSessionsFromPostgres(Guid? userId)
    {
        var query = _postgresContext.UserSessions.Where(x => !x.IsDeleted);
        if (userId.HasValue)
            query = query.Where(x => x.UserId == userId.Value);
        
        var sessions = await query.ToListAsync();
        foreach (var session in sessions)
        {
            var sqliteSession = await _sqliteContext.UserSessions.FirstOrDefaultAsync(x => x.Id == session.Id);
            if (sqliteSession == null)
            {
                var newSession = CloneEntityForSqlite(session);
                newSession.IsSynced = true;
                _sqliteContext.UserSessions.Add(newSession);
            }
            else if (session.LastModified > sqliteSession.LastModified)
            {
                CopyEntityProperties(session, sqliteSession);
                sqliteSession.IsSynced = true;
            }
        }
        await _sqliteContext.SaveChangesAsync();
    }
}