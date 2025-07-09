using Tasker.Cli.Setup;
using Tasker.Cli.UI;
using Tasker.Infrastructure.Data;
using Spectre.Console;

if (!await SetupUI.EnsureDatabaseConfiguredAsync())
    return;

var (mainMenu, taskService, projectService, loginUI, _, syncService) = ServiceContainer.CreateServices();

// Pre-login sync: Handle username conflicts and sync SQLite users to PostgreSQL
await HandlePreLoginSync(syncService);

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

static async Task HandlePreLoginSync(SyncService syncService)
{
    // Check if PostgreSQL is available
    if (!await syncService.IsPostgresAvailableAsync())
        return; // SQLite-only mode, no conflicts to resolve

    try
    {
        // Check for username conflicts and resolve them
        await syncService.HandleUsernameConflictsAsync();
        
        // Sync SQLite users to PostgreSQL (without requiring login)
        await syncService.SyncToPostgresAsync();
    }
    catch (Exception ex)
    {
        // Show error but don't crash the app
        AnsiConsole.MarkupLine($"[yellow]Warning: Pre-login sync failed: {ex.Message}[/]");
        AnsiConsole.WriteLine("Press any key to continue...");
        Console.ReadKey();
    }
}