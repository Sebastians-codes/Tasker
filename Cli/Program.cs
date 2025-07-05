using Tasker.Cli.Setup;
using Tasker.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

var (mainMenu, taskService, projectService, projectCommands, taskCommands, gitService) = ServiceContainer.CreateServices();

gitService.Pull();

var factory = new DesignTimeDbContextFactory();
using var context = factory.CreateDbContext([]);
await context.Database.MigrateAsync();

if (args.Length > 0)
{
    if (args[0].StartsWith('p'))
        await projectCommands.Router(args);
    else
        await taskCommands.Router(args);
}
else
    await mainMenu.ShowMenuAsync();

gitService.Push();