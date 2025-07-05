using Spectre.Console;
using Tasker.Cli.Services;
using Tasker.Domain.Models;
using System.Security.Cryptography;
using System.Text;

namespace Tasker.Cli.UI;

public class LoginUI(IUserService userService)
{
    private readonly IUserService _userService = userService;

    public async Task<User?> ShowLoginAsync()
    {
        while (true)
        {
            AnsiConsole.Clear();
            AnsiConsole.Write(new FigletText("Tasker Login").Centered().Color(Color.Blue));
            AnsiConsole.WriteLine();

            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .AddChoices([
                        "Login",
                        "Register", 
                        "Exit"
                    ]));

            switch (choice)
            {
                case "Login":
                    var loginResult = await HandleLoginAsync();
                    if (loginResult != null)
                        return loginResult;
                    break;
                case "Register":
                    await HandleRegisterAsync();
                    break;
                case "Exit":
                    return null;
            }
        }
    }

    private async Task<User?> HandleLoginAsync()
    {
        AnsiConsole.Clear();
        AnsiConsole.Write(new Rule("[blue]Login[/]").LeftJustified());
        AnsiConsole.WriteLine();

        var username = AnsiConsole.Ask<string>("Username:");
        var password = AnsiConsole.Prompt(
            new TextPrompt<string>("Password:")
                .Secret());

        var user = await _userService.AuthenticateAsync(username, password);
        
        if (user != null)
        {
            AnsiConsole.MarkupLine($"[green]Welcome back, {user.Username}![/]");
            AnsiConsole.WriteLine("Press any key to continue...");
            Console.ReadKey();
            return user;
        }

        AnsiConsole.MarkupLine("[red]Invalid username or password.[/]");
        AnsiConsole.WriteLine("Press any key to continue...");
        Console.ReadKey();
        return null;
    }

    private async Task HandleRegisterAsync()
    {
        AnsiConsole.Clear();
        AnsiConsole.Write(new Rule("[green]Register[/]").LeftJustified());
        AnsiConsole.WriteLine();

        var username = AnsiConsole.Ask<string>("Username:");
        
        if (await _userService.UsernameExistsAsync(username))
        {
            AnsiConsole.MarkupLine("[red]Username already exists.[/]");
            AnsiConsole.WriteLine("Press any key to continue...");
            Console.ReadKey();
            return;
        }

        var password = AnsiConsole.Prompt(
            new TextPrompt<string>("Password:")
                .Secret());

        var confirmPassword = AnsiConsole.Prompt(
            new TextPrompt<string>("Confirm Password:")
                .Secret());

        if (password != confirmPassword)
        {
            AnsiConsole.MarkupLine("[red]Passwords do not match.[/]");
            AnsiConsole.WriteLine("Press any key to continue...");
            Console.ReadKey();
            return;
        }

        try
        {
            await _userService.RegisterAsync(username, password);
            AnsiConsole.MarkupLine($"[green]User {username} registered successfully![/]");
            AnsiConsole.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Registration failed: {ex.Message}[/]");
            AnsiConsole.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }
    }
}