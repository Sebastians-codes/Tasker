using Tasker.Core.Interfaces;
using Tasker.Infrastructure.Data;
using Tasker.Infrastructure.Repositories;
using Tasker.Cli.Services;
using Tasker.Cli.UI;
using Tasker.Cli.UI.Cli;
using Tasker.Cli.Models;
using Microsoft.EntityFrameworkCore;

namespace Tasker.Cli.Setup;

public static class ServiceContainer
{
    public static (MainMenu mainMenu, ITaskService taskService, IProjectService projectService, ProjectCommands projectCommands, TaskCommands taskCommands, LoginUI loginUI, IUserService userService) CreateServices()
    {
        var context = CreateDbContext();

        var taskRepository = new TaskRepository(context);
        var projectRepository = new ProjectRepository(context);
        var userRepository = new UserRepository(context);

        var userService = new UserService(userRepository);
        var sessionService = new SessionService(userRepository);
        var taskService = new TaskService(taskRepository);
        var projectService = new ProjectService(projectRepository);

        var taskDisplay = new TaskDisplay();
        var projectDisplay = new ProjectDisplay(taskDisplay);
        var taskMenu = new TaskMenu(taskService, projectService, taskDisplay);
        var projectMenu = new ProjectMenu(projectService, projectDisplay, taskMenu, taskService, taskDisplay);
        var mainMenu = new MainMenu(taskMenu, projectMenu, sessionService);

        var projectCommands = new ProjectCommands(projectDisplay, projectRepository, projectMenu);

        var taskCommands = new TaskCommands(taskDisplay, taskRepository, taskMenu);

        var loginUI = new LoginUI(userService, sessionService);

        return (mainMenu, taskService, projectService, projectCommands, taskCommands, loginUI, userService);
    }

    public static TaskerDbContext CreateDbContext()
    {
        var connectionString = GetConnectionString();
        var optionsBuilder = new DbContextOptionsBuilder<TaskerDbContext>();
        
        optionsBuilder.UseNpgsql(connectionString);

        return new TaskerDbContext(optionsBuilder.Options);
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
                    // Remove quotes if they were accidentally included
                    decryptedConnectionString = decryptedConnectionString.Trim('"', '\'');
                    return decryptedConnectionString;
                }
            }
        }
        catch
        {
            // If anything fails, fall back to design-time connection string
        }

        // Use the same connection string as DesignTimeDbContextFactory for consistency
        return "Host=localhost;Database=tasker;Username=postgres;Password=password";
    }
}
