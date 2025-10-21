using Tasker.Cli.Setup;
using Tasker.Cli.UI;
using Tasker.Infrastructure.Data;

if (!await SetupUI.EnsureDatabaseConfiguredAsync())
    return;

var (mainMenu, taskService, projectService, loginUI, _, syncService) = ServiceContainer.CreateServices();

await HandlePreLoginSync(syncService);

var currentUser = await loginUI.ShowLoginAsync();
if (currentUser == null)
    return;

taskService.SetCurrentUser(currentUser);
projectService.SetCurrentUser(currentUser);

_ = Task.Run(async () =>
{
    await syncService.SyncToPostgresAsync(currentUser.Id);
    await syncService.FullSyncFromPostgresAsync(currentUser.Id);
});

await mainMenu.ShowMenuAsync();

static async Task HandlePreLoginSync(SyncService syncService)
{
    if (!await syncService.IsPostgresAvailableAsync())
        return;

    try
    {
        await syncService.HandleUsernameConflictsAsync();
        await syncService.SyncToPostgresAsync();
    }
    catch
    {
    }
}
