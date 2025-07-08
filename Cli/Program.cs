using Tasker.Cli.Setup;
using Tasker.Cli.UI;

if (!await SetupUI.EnsureDatabaseConfiguredAsync())
    return;

var (mainMenu, taskService, projectService, projectCommands, taskCommands, loginUI, _) = ServiceContainer.CreateServices();

var currentUser = await loginUI.ShowLoginAsync();
if (currentUser == null)
    return;

taskService.SetCurrentUser(currentUser);
projectService.SetCurrentUser(currentUser);

if (args.Length > 0)
{
    if (args[0].StartsWith('p'))
        await projectCommands.Router(args);
    else
        await taskCommands.Router(args);
}
else
    await mainMenu.ShowMenuAsync();