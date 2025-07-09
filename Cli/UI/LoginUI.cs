using Spectre.Console;
using Tasker.Cli.Services;
using Tasker.Domain.Models;
using Tasker.Cli.Models;
using Tasker.Cli.Helpers;
using System.Security;
using System.Runtime.InteropServices;

namespace Tasker.Cli.UI;

public class LoginUI(IUserService userService, SessionService sessionService)
{
    private readonly IUserService _userService = userService;
    private readonly SessionService _sessionService = sessionService;

    public async Task<User?> ShowLoginAsync()
    {
        var config = AppConfig.Load();

        if (!string.IsNullOrEmpty(config.SessionToken))
        {
            var (decryptedToken, tokenExpiry) = EncryptionService.DecryptToken(config.SessionToken);
            if (!string.IsNullOrEmpty(decryptedToken))
            {
                var session = await _sessionService.ValidateSessionAsync(decryptedToken);
                if (session != null && session.AutoLoginEnabled)
                {
                    AnsiConsole.Clear();
                    AnsiConsole.Write(new FigletText("Tasker Login").Centered().Color(Color.Blue));
                    AnsiConsole.WriteLine();
                    AnsiConsole.MarkupLine($"[green]Auto-logging in as {session.User.Username}...[/]");
                    Thread.Sleep(1000);
                    return session.User;
                }
            }

            config.SessionToken = null;
            config.Save();
        }

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
                    {
                        var session = await _sessionService.CreateSessionAsync(loginResult, 30, false);
                        config.SessionToken = EncryptionService.EncryptToken(session.Token, session.ExpiresAt);
                        config.Save();
                        return loginResult;
                    }
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
        var password = InputParser.GetPasswordWithEscapeHandling("Password");
        
        if (password == null)
        {
            AnsiConsole.MarkupLine("[yellow]Login cancelled.[/]");
            AnsiConsole.WriteLine("Press any key to continue...");
            Console.ReadKey();
            return null;
        }

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

        // Get username with validation
        string? username;
        while (true)
        {
            username = InputParser.GetInputWithEscapeHandling("Username");
            
            // Check for ESC cancellation
            if (username == null)
            {
                AnsiConsole.MarkupLine("[yellow]Registration cancelled.[/]");
                AnsiConsole.WriteLine("Press any key to continue...");
                Console.ReadKey();
                return;
            }
            
            // Check if username already exists
            if (await _userService.UsernameExistsAsync(username))
            {
                AnsiConsole.MarkupLine($"[red]Username '{username}' already exists. Please choose a different username.[/]");
                continue;
            }
            break;
        }

        var password = InputParser.GetPasswordWithEscapeHandling("Password");

        if (password == null)
        {
            AnsiConsole.MarkupLine("[yellow]Registration cancelled.[/]");
            AnsiConsole.WriteLine("Press any key to continue...");
            Console.ReadKey();
            return;
        }

        var confirmPassword = InputParser.GetPasswordWithEscapeHandling("Confirm Password");

        if (confirmPassword == null)
        {
            password.Dispose();
            AnsiConsole.MarkupLine("[yellow]Registration cancelled.[/]");
            AnsiConsole.WriteLine("Press any key to continue...");
            Console.ReadKey();
            return;
        }

        // Compare passwords manually since SecureString doesn't have comparison
        if (!SecureStringEqual(password, confirmPassword))
        {
            password.Dispose();
            confirmPassword.Dispose();
            AnsiConsole.MarkupLine("[red]Passwords do not match.[/]");
            AnsiConsole.WriteLine("Press any key to continue...");
            Console.ReadKey();
            return;
        }

        confirmPassword.Dispose(); // We don't need this anymore

        try
        {
            await _userService.RegisterAsync(username, password);
            AnsiConsole.MarkupLine($"[green]User {username} registered successfully![/]");
            AnsiConsole.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }
        catch (Exception ex)
        {
            password.Dispose();
            AnsiConsole.MarkupLine($"[red]Registration failed: {ex.Message}[/]");
            AnsiConsole.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }
    }

    private static bool SecureStringEqual(SecureString ss1, SecureString ss2)
    {
        if (ss1.Length != ss2.Length)
            return false;

        IntPtr ptr1 = IntPtr.Zero;
        IntPtr ptr2 = IntPtr.Zero;

        try
        {
            ptr1 = Marshal.SecureStringToGlobalAllocUnicode(ss1);
            ptr2 = Marshal.SecureStringToGlobalAllocUnicode(ss2);

            unsafe
            {
                char* p1 = (char*)ptr1;
                char* p2 = (char*)ptr2;

                for (int i = 0; i < ss1.Length; i++)
                {
                    if (p1[i] != p2[i])
                        return false;
                }
            }

            return true;
        }
        finally
        {
            if (ptr1 != IntPtr.Zero)
                Marshal.ZeroFreeGlobalAllocUnicode(ptr1);
            if (ptr2 != IntPtr.Zero)
                Marshal.ZeroFreeGlobalAllocUnicode(ptr2);
        }
    }
}