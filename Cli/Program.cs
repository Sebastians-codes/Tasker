using Tasker.Cli.Setup;
using Tasker.Cli.UI;

if (!await SetupUI.EnsureDatabaseConfiguredAsync())
    return;

var (mainMenu, taskService, projectService, loginUI, _, syncService) = ServiceContainer.CreateServices();

var currentUser = await loginUI.ShowLoginAsync();
if (currentUser == null)
    return;

taskService.SetCurrentUser(currentUser);
projectService.SetCurrentUser(currentUser);

// Sync data after successful login
_ = Task.Run(async () =>
{
    await syncService.SyncToPostgresAsync(currentUser.Id);
    await syncService.FullSyncFromPostgresAsync(currentUser.Id);
});

await mainMenu.ShowMenuAsync();