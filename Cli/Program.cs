using Tasker.Cli.Setup;
using Tasker.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Tasker.Cli.Models;
using Tasker.Cli.Services;
using Spectre.Console;

// Check if this is first run (no database connection configured)
var config = AppConfig.Load();
if (string.IsNullOrEmpty(config.EncryptedConnectionString))
{
    AnsiConsole.Clear();
    AnsiConsole.Write(new FigletText("Tasker Setup").Centered().Color(Color.Green));
    AnsiConsole.WriteLine();
    AnsiConsole.MarkupLine("[yellow]Welcome to Tasker! This appears to be your first time running the application.[/]");
    AnsiConsole.MarkupLine("[yellow]Please configure your database connection:[/]");
    AnsiConsole.WriteLine();

    var databaseChoice = AnsiConsole.Prompt(
        new SelectionPrompt<string>()
            .Title("Choose your database:")
            .AddChoices([
                "SQLite (Local file - Recommended for personal use)",
                "PostgreSQL (Remote server)",
                "Configure later (Use temporary SQLite)"
            ]));

    if (databaseChoice.StartsWith("PostgreSQL"))
    {
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[yellow]Enter your PostgreSQL connection string:[/]");
        AnsiConsole.MarkupLine("[dim]Example: Host=localhost;Port=5432;Database=tasker;Username=user;Password=pass[/]");
        AnsiConsole.WriteLine();

        var connectionString = AnsiConsole.Prompt(
            new TextPrompt<string>("Connection String:")
                .Secret('*'));

        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            var encryptedConnectionString = EncryptionService.EncryptConnectionString(connectionString);
            if (!string.IsNullOrEmpty(encryptedConnectionString))
            {
                config.EncryptedConnectionString = encryptedConnectionString;
                config.Save();
                AnsiConsole.MarkupLine("[green]✓ Database connection saved successfully![/]");
            }
            else
            {
                AnsiConsole.MarkupLine("[red]Failed to encrypt connection string. Using SQLite as fallback.[/]");
            }
        }
        else
        {
            AnsiConsole.MarkupLine("[yellow]Empty connection string. Using SQLite as fallback.[/]");
        }
    }
    else if (databaseChoice.StartsWith("SQLite"))
    {
        AnsiConsole.MarkupLine("[green]✓ SQLite selected. Database will be created automatically.[/]");
    }
    else
    {
        AnsiConsole.MarkupLine("[yellow]Database configuration skipped. Using temporary SQLite.[/]");
        AnsiConsole.MarkupLine("[dim]You can configure the database later in Settings → Database Settings[/]");
    }

    AnsiConsole.WriteLine();
    AnsiConsole.MarkupLine("[dim]Press any key to continue...[/]");
    Console.ReadKey();
}

var (mainMenu, taskService, projectService, projectCommands, taskCommands, loginUI, userService) = ServiceContainer.CreateServices();

var factory = new DesignTimeDbContextFactory();
using var context = factory.CreateDbContext([]);
await context.Database.MigrateAsync();

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