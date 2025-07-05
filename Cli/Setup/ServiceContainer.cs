using Tasker.Core.Interfaces;
using Tasker.Infrastructure.Data;
using Tasker.Infrastructure.Repositories;
using Tasker.Cli.Services;
using Tasker.Cli.UI;
using Tasker.Cli.UI.Cli;

namespace Tasker.Cli.Setup;

public static class ServiceContainer
{
    public static (MainMenu mainMenu, ITaskService taskService, IProjectService projectService, ProjectCommands projectCommands, TaskCommands taskCommands, GitService gitService, LoginUI loginUI, IUserService userService) CreateServices()
    {
        var factory = new DesignTimeDbContextFactory();
        var context = factory.CreateDbContext([]);

        var taskRepository = new TaskRepository(context);
        var projectRepository = new ProjectRepository(context);
        var userRepository = new UserRepository(context);

        var userService = new UserService(userRepository);
        var taskService = new TaskService(taskRepository);
        var projectService = new ProjectService(projectRepository);

        var taskDisplay = new TaskDisplay();
        var projectDisplay = new ProjectDisplay(taskDisplay);
        var taskMenu = new TaskMenu(taskService, projectService, taskDisplay);
        var projectMenu = new ProjectMenu(projectService, projectDisplay, taskMenu, taskService, taskDisplay);
        var mainMenu = new MainMenu(taskMenu, projectMenu);

        var projectCommands = new ProjectCommands(projectDisplay, projectRepository, projectMenu);

        var taskCommands = new TaskCommands(taskDisplay, taskRepository, taskMenu);

        var gitService = new GitService();
        var loginUI = new LoginUI(userService);

        return (mainMenu, taskService, projectService, projectCommands, taskCommands, gitService, loginUI, userService);
    }
}
