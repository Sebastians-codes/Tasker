using Tasker.Core.Interfaces;
using Tasker.Infrastructure.Data;
using Tasker.Infrastructure.Repositories;
using Tasker.Cli.Services;
using Tasker.Cli.UI;
using Tasker.Cli.Models;
using Microsoft.EntityFrameworkCore;

namespace Tasker.Cli.Setup;

public static class ServiceContainer
{
    public static (MainMenu mainMenu, ITaskService taskService, IProjectService projectService, LoginUI loginUI, IUserService userService, SyncService syncService) CreateServices()
    {
        var (postgresContext, sqliteContext, connectionMonitor, syncService) = CreateDatabaseServices();
        var databaseManager = new DatabaseManager(postgresContext, sqliteContext, connectionMonitor);

        var taskRepository = new TaskRepository(databaseManager);
        var projectRepository = new ProjectRepository(databaseManager);
        var userRepository = new UserRepository(databaseManager);

        var userService = new UserService(userRepository);
        var sessionService = new SessionService(userRepository);
        var taskService = new TaskService(taskRepository);
        var projectService = new ProjectService(projectRepository);

        var taskDisplay = new TaskDisplay();
        var projectDisplay = new ProjectDisplay(taskDisplay);
        var taskMenu = new TaskMenu(taskService, projectService, taskDisplay);
        var projectMenu = new ProjectMenu(projectService, projectDisplay, taskMenu, taskService, taskDisplay);
        var mainMenu = new MainMenu(taskMenu, projectMenu, sessionService);

        var loginUI = new LoginUI(userService, sessionService);

        return (mainMenu, taskService, projectService, loginUI, userService, syncService);
    }

    public static (PostgresDbContext postgresContext, SqliteDbContext sqliteContext, IConnectionMonitor connectionMonitor, SyncService syncService) CreateDatabaseServices()
    {
        var postgresConnectionString = GetConnectionString();
        var sqliteConnectionString = GetSqliteConnectionString();

        var postgresOptions = new DbContextOptionsBuilder<PostgresDbContext>()
            .UseNpgsql(postgresConnectionString)
            .Options;

        var sqliteOptions = new DbContextOptionsBuilder<SqliteDbContext>()
            .UseSqlite(sqliteConnectionString)
            .Options;

        var postgresContext = new PostgresDbContext(postgresOptions);
        var sqliteContext = new SqliteDbContext(sqliteOptions);
        var connectionMonitor = new ConnectionMonitor(postgresContext);
        var syncService = new SyncService(postgresContext, sqliteContext, connectionMonitor);

        try
        {
            postgresContext.Database.Migrate();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"PostgreSQL migration failed: {ex.Message}");
        }

        try
        {
            sqliteContext.Database.Migrate();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"SQLite migration failed: {ex.Message}");
        }

        return (postgresContext, sqliteContext, connectionMonitor, syncService);
    }


    private static string GetSqliteConnectionString()
    {
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Tasker");

        if (!Directory.Exists(appDataPath))
            Directory.CreateDirectory(appDataPath);

        var dbPath = Path.Combine(appDataPath, "tasker_local.db");
        return $"Data Source={dbPath}";
    }

    private static string GetConnectionString()
    {
        try
        {
            var config = AppConfig.Load();

            if (!string.IsNullOrEmpty(config.EncryptedConnectionString))
            {
                var decryptedConnectionString = EncryptionService.DecryptConnectionString(config.EncryptedConnectionString);

                if (!string.IsNullOrEmpty(decryptedConnectionString))
                {
                    decryptedConnectionString = decryptedConnectionString.Trim('"', '\'');
                    return decryptedConnectionString;
                }
            }
        }
        catch
        {
        }

        return "Host=localhost;Database=tasker;Username=postgres;Password=password";
    }
}
