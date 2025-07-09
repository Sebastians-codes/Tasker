using Spectre.Console;
using Tasker.Cli.Models;
using Tasker.Cli.Services;
using Microsoft.EntityFrameworkCore;
using Tasker.Infrastructure.Data;

namespace Tasker.Cli.UI;

public class MainMenu(TaskMenu taskMenu, ProjectMenu projectMenu, SessionService sessionService)
{
    private readonly TaskMenu _taskMenu = taskMenu;
    private readonly ProjectMenu _projectMenu = projectMenu;
    private readonly SessionService _sessionService = sessionService;

    public async Task ShowMenuAsync()
    {
        while (true)
        {
            AnsiConsole.Clear();
            AnsiConsole.Write(new FigletText("Tasker").Centered().Color(Color.Orange1));
            AnsiConsole.WriteLine();

            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .AddChoices([
                        "Tasks",
                        "Projects",
                        "Settings",
                        "Exit"
                    ]));

            switch (choice)
            {
                case "Tasks":
                    await _taskMenu.ShowMenuAsync();
                    break;
                case "Projects":
                    await _projectMenu.ShowMenuAsync();
                    break;
                case "Settings":
                    await ShowSettingsMenuAsync();
                    break;
                case "Exit":
                    return;
            }
        }
    }

    private async Task ShowSettingsMenuAsync()
    {
        while (true)
        {
            AnsiConsole.Clear();
            AnsiConsole.Write(new FigletText("Settings").Centered().Color(Color.Green));
            AnsiConsole.WriteLine();

            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .AddChoices([
                        "Login Settings",
                        "Database Settings",
                        "Back"
                    ]));

            switch (choice)
            {
                case "Login Settings":
                    await ShowLoginSettingsMenuAsync();
                    break;
                case "Database Settings":
                    await ShowDatabaseSettingsMenuAsync();
                    break;
                case "Back":
                    return;
            }
        }
    }

    private async Task ShowLoginSettingsMenuAsync()
    {
        while (true)
        {
            AnsiConsole.Clear();
            AnsiConsole.Write(new FigletText("Login Settings").Centered().Color(Color.Blue));
            AnsiConsole.WriteLine();

            var config = AppConfig.Load();
            Domain.Models.UserSession? currentSession = null;

            if (!string.IsNullOrEmpty(config.SessionToken))
            {
                var (decryptedToken, tokenExpiry) = EncryptionService.DecryptToken(config.SessionToken);
                if (!string.IsNullOrEmpty(decryptedToken))
                {
                    currentSession = await _sessionService.ValidateSessionAsync(decryptedToken);
                }
            }

            if (currentSession != null)
            {
                var autoLoginStatus = currentSession.AutoLoginEnabled ? "[green]✓ Enabled[/]" : "[red]✗ Disabled[/]";
                var expiresAt = currentSession.ExpiresAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");

                AnsiConsole.MarkupLine($"Auto-login: {autoLoginStatus}");
                AnsiConsole.MarkupLine($"Session Duration: [cyan]{currentSession.DurationDays} days[/]");
                AnsiConsole.MarkupLine($"Expires: [yellow]{expiresAt}[/]");

                AnsiConsole.WriteLine();

                var choice = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .AddChoices([
                            "Toggle Auto-Login",
                            "Change Session Duration",
                            "Back"
                        ]));

                switch (choice)
                {
                    case "Toggle Auto-Login":
                        await ToggleAutoLoginAsync(config, currentSession);
                        break;
                    case "Change Session Duration":
                        await ChangeSessionDurationAsync(config, currentSession);
                        break;
                    case "Back":
                        return;
                }
            }
            else
            {
                AnsiConsole.MarkupLine("[yellow]No active session - please log in to configure auto-login[/]");
                AnsiConsole.WriteLine();

                var choice = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .AddChoices([
                            "Back"
                        ]));

                if (choice == "Back")
                    return;
            }
        }
    }

    private async Task ToggleAutoLoginAsync(AppConfig config, Domain.Models.UserSession? currentSession)
    {
        if (currentSession == null)
        {
            AnsiConsole.MarkupLine("[red]No active session to modify[/]");
            await Task.Delay(1500);
            return;
        }

        var newAutoLoginStatus = !currentSession.AutoLoginEnabled;
        var (decryptedToken, _) = EncryptionService.DecryptToken(config.SessionToken!);
        await _sessionService.UpdateSessionSettingsAsync(decryptedToken, autoLoginEnabled: newAutoLoginStatus);

        var status = newAutoLoginStatus ? "[green]enabled[/]" : "[red]disabled[/]";
        AnsiConsole.MarkupLine($"Auto-login {status}!");
        await Task.Delay(1000);
    }

    private async Task ChangeSessionDurationAsync(AppConfig config, Domain.Models.UserSession? currentSession)
    {
        if (currentSession == null)
        {
            AnsiConsole.MarkupLine("[red]No active session to modify[/]");
            await Task.Delay(1500);
            return;
        }

        var durationChoice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select session duration:")
                .AddChoices([
                    "7 days",
                    "30 days",
                    "90 days",
                    "365 days (1 year)",
                    "Cancel"
                ]));

        if (durationChoice == "Cancel")
            return;

        var days = durationChoice switch
        {
            "7 days" => 7,
            "30 days" => 30,
            "90 days" => 90,
            "365 days (1 year)" => 365,
            _ => 30
        };

        var (decryptedToken, _) = EncryptionService.DecryptToken(config.SessionToken!);
        await _sessionService.UpdateSessionSettingsAsync(decryptedToken, durationDays: days);
        AnsiConsole.MarkupLine($"[green]Session duration updated to {days} days![/]");
        await Task.Delay(1000);
    }

    private async Task ShowDatabaseSettingsMenuAsync()
    {
        while (true)
        {
            AnsiConsole.Clear();
            AnsiConsole.Write(new FigletText("Database Settings").Centered().Color(Color.Blue));
            AnsiConsole.WriteLine();

            var config = AppConfig.Load();
            var hasConnectionString = !string.IsNullOrEmpty(config.EncryptedConnectionString);

            if (hasConnectionString)
            {
                var decryptedConnectionString = EncryptionService.DecryptConnectionString(config.EncryptedConnectionString!);
                if (!string.IsNullOrEmpty(decryptedConnectionString))
                {
                    var maskedConnectionString = MaskConnectionString(decryptedConnectionString);
                    AnsiConsole.MarkupLine($"[green]✓[/] Connection configured: [yellow]{maskedConnectionString}[/]");
                }
                else
                {
                    AnsiConsole.MarkupLine("[red]✗[/] Connection string configured but cannot decrypt (wrong machine?)");
                }
            }
            else
            {
                AnsiConsole.MarkupLine("[yellow]No PostgreSQL database connection configured[/]");
            }

            AnsiConsole.WriteLine();

            var choices = new List<string> { "Set Database Connection", "Back" };
            if (hasConnectionString)
            {
                choices.Insert(1, "Remove Connection");
            }

            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .AddChoices(choices));

            switch (choice)
            {
                case "Set Database Connection":
                    var connectionChanged = await SetDatabaseConnectionAsync(config);
                    if (connectionChanged)
                    {
                        await ShowRestartRequiredAndExit();
                        return;
                    }
                    break;
                case "Remove Connection":
                    config.EncryptedConnectionString = null;
                    config.Save();
                    AnsiConsole.MarkupLine("[yellow]Database connection removed.[/]");
                    await Task.Delay(1500);
                    await ShowRestartRequiredAndExit();
                    return;
                case "Back":
                    return;
            }
        }
    }

    private async Task<bool> SetDatabaseConnectionAsync(AppConfig config)
    {
        AnsiConsole.Clear();
        AnsiConsole.Write(new Rule("[cyan]Database Connection Setup[/]").LeftJustified());
        AnsiConsole.WriteLine();

        AnsiConsole.MarkupLine("[yellow]Enter your PostgreSQL connection string:[/]");
        AnsiConsole.MarkupLine("[dim]Example: Host=localhost;Port=5432;Database=tasker;Username=user;Password=pass[/]");
        AnsiConsole.WriteLine();

        var connectionString = AnsiConsole.Prompt(
            new TextPrompt<string>("Connection String:")
                .Secret('*'));

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            AnsiConsole.MarkupLine("[red]Connection string cannot be empty.[/]");
            await Task.Delay(1500);
            return false;
        }

        AnsiConsole.MarkupLine("[blue]Testing database connection...[/]");

        try
        {
            var testOptions = new DbContextOptionsBuilder<PostgresDbContext>()
                .UseNpgsql(connectionString)
                .Options;

            using var testContext = new PostgresDbContext(testOptions);
            await testContext.Database.CanConnectAsync();

            var encryptedConnectionString = EncryptionService.EncryptConnectionString(connectionString);
            if (string.IsNullOrEmpty(encryptedConnectionString))
            {
                AnsiConsole.MarkupLine("[red]Failed to encrypt connection string.[/]");
                await Task.Delay(1500);
                return false;
            }

            config.EncryptedConnectionString = encryptedConnectionString;
            config.Save();

            AnsiConsole.MarkupLine("[green]✓ Database connection saved successfully![/]");
            AnsiConsole.MarkupLine("[dim]Connection string is encrypted and stored securely.[/]");
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[orange1]⚠️ Database settings have been changed.[/]");
            AnsiConsole.MarkupLine("[orange1]⚠️ The application will now restart to apply the new settings.[/]");
            AnsiConsole.MarkupLine("[orange1]⚠️ You will need to log in again.[/]");
            
            return true;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]✗ Failed to connect to database: {ex.Message}[/]");
            AnsiConsole.MarkupLine("[yellow]Connection string was not saved.[/]");
            await Task.Delay(3000);
            return false;
        }
    }

    private static string MaskConnectionString(string connectionString)
    {
        try
        {
            var parts = connectionString.Split(';');
            var hostPart = parts.FirstOrDefault(p => p.Trim().StartsWith("Host=", StringComparison.OrdinalIgnoreCase));
            var databasePart = parts.FirstOrDefault(p => p.Trim().StartsWith("Database=", StringComparison.OrdinalIgnoreCase));

            if (hostPart != null && databasePart != null)
            {
                var host = hostPart.Split('=')[1].Trim();
                var database = databasePart.Split('=')[1].Trim();
                return $"{host}/{database}";
            }
        }
        catch
        {
        }

        return "***configured***";
    }

    private async Task ShowRestartRequiredAndExit()
    {
        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Rule("[red]Application Restart Required[/]").Centered());
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[red]The application will now shut down to apply the new database settings.[/]");
        AnsiConsole.MarkupLine("[yellow]You will need to log in again.[/]");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[dim]Press any key to exit...[/]");
        Console.ReadKey();
        Environment.Exit(0);
    }
}