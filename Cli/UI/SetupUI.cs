using Spectre.Console;
using Tasker.Cli.Models;
using Tasker.Cli.Services;

namespace Tasker.Cli.UI;

public static class SetupUI
{
    public static async Task<bool> EnsureDatabaseConfiguredAsync()
    {
        var config = AppConfig.Load();
        if (!string.IsNullOrEmpty(config.EncryptedConnectionString))
            return true;

        return await ShowFirstTimeSetupAsync(config);
    }

    private static async Task<bool> ShowFirstTimeSetupAsync(AppConfig config)
    {
        AnsiConsole.Clear();
        AnsiConsole.Write(new FigletText("Tasker Setup").Centered().Color(Color.Green));
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[yellow]Welcome to Tasker! This appears to be your first time running the application.[/]");
        AnsiConsole.WriteLine();

        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("How would you like to run Tasker?")
                .AddChoices([
                    "Connect to PostgreSQL database",
                    "Run locally only (SQLite only)"
                ]));

        string connectionString;
        
        if (choice == "Connect to PostgreSQL database")
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[yellow]Enter your PostgreSQL connection string:[/]");
            AnsiConsole.MarkupLine("[dim]Example: Host=localhost;Port=5432;Database=tasker;Username=user;Password=pass[/]");
            AnsiConsole.WriteLine();

            connectionString = AnsiConsole.Prompt(
                new TextPrompt<string>("Connection String:")
                    .Secret('*'));

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                AnsiConsole.MarkupLine("[yellow]Connection string is required.[/]");
                await Task.Delay(2000);
                return false;
            }
        }
        else
        {
            // Local only mode - use a dummy connection string
            connectionString = "local_only_mode";
            AnsiConsole.MarkupLine("[green]✓ Configured for local-only mode (SQLite only)[/]");
        }

        var encryptedConnectionString = EncryptionService.EncryptConnectionString(connectionString);
        if (!string.IsNullOrEmpty(encryptedConnectionString))
        {
            config.EncryptedConnectionString = encryptedConnectionString;
            config.Save();
            
            if (choice == "Connect to PostgreSQL database")
            {
                AnsiConsole.MarkupLine("[green]✓ Database connection saved successfully![/]");
            }
            
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[dim]Press any key to continue...[/]");
            Console.ReadKey();
            return true;
        }
        else
        {
            AnsiConsole.MarkupLine("[red]Failed to encrypt connection string.[/]");
            await Task.Delay(2000);
            return false;
        }
    }
}