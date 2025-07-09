using System.Globalization;
using System.Text.RegularExpressions;
using Spectre.Console;

namespace Tasker.Cli.Helpers;

public static class InputParser
{
    public static DateTime? ParseDate(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;

        if (Regex.IsMatch(input, @"^\d{1,2}/\d{1,2}$"))
        {
            var parts = input.Split('/');
            if (parts.Length == 2 &&
                int.TryParse(parts[0], out int day) &&
                int.TryParse(parts[1], out int month))
            {
                var currentYear = DateTime.UtcNow.Year;
                try
                {
                    var date = DateTime.SpecifyKind(new DateTime(currentYear, month, day), DateTimeKind.Utc);

                    if (date < DateTime.Today)
                        date = DateTime.SpecifyKind(new DateTime(currentYear + 1, month, day), DateTimeKind.Utc);

                    return date;
                }
                catch (ArgumentOutOfRangeException)
                {
                    return null;
                }
            }
        }

        if (DateTime.TryParse(input, out DateTime result))
        {
            var currentYear = DateTime.UtcNow.Year;
            if (result.Year < currentYear)
                return null;

            if (result.Year > currentYear + 10)
            {
                return null;
            }

            return DateTime.SpecifyKind(result, DateTimeKind.Utc);
        }

        return null;
    }

    public static int? ParseTimeEstimate(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;

        input = input.Trim().ToLower();

        var timePattern = @"^(?:(\d+(?:\.\d+)?)h)?(?:\s*(\d+)m)?$";
        var match = Regex.Match(input, timePattern);

        if (match.Success)
        {
            int totalMinutes = 0;

            if (match.Groups[1].Success && double.TryParse(match.Groups[1].Value, out double hours))
                totalMinutes += (int)(hours * 60);

            if (match.Groups[2].Success && int.TryParse(match.Groups[2].Value, out int minutes))
                totalMinutes += minutes;

            return totalMinutes > 0 ? totalMinutes : null;
        }

        if (int.TryParse(input, out int plainMinutes))
            return plainMinutes;

        return null;
    }

    public static string? GetInputWithEscapeHandling(string prompt, bool allowEmpty = false)
    {
        while (true)
        {
            AnsiConsole.Write($"{prompt}: ");
            AnsiConsole.MarkupLine("[dim]ESC to cancel[/]");
            AnsiConsole.Write("> ");
            
            var input = "";
            ConsoleKeyInfo key;
            
            while (true)
            {
                key = Console.ReadKey(true);
                
                if (key.Key == ConsoleKey.Escape)
                {
                    Console.WriteLine();
                    return null; // ESC pressed - cancellation
                }
                else if (key.Key == ConsoleKey.Enter)
                {
                    break;
                }
                else if (key.Key == ConsoleKey.Backspace)
                {
                    if (input.Length > 0)
                    {
                        input = input[..^1];
                        Console.Write("\b \b");
                    }
                }
                else if (!char.IsControl(key.KeyChar))
                {
                    input += key.KeyChar;
                    Console.Write(key.KeyChar);
                }
            }
            
            Console.WriteLine();
            input = input.Trim();
            
            // If input is empty and not allowed, prompt again
            if (string.IsNullOrWhiteSpace(input) && !allowEmpty)
            {
                AnsiConsole.MarkupLine("[red]Please enter a value.[/]");
                continue;
            }
            
            return input;
        }
    }

    public static string? GetPasswordWithEscapeHandling(string prompt)
    {
        while (true)
        {
            AnsiConsole.Write($"{prompt}: ");
            AnsiConsole.MarkupLine("[dim]ESC to cancel[/]");
            AnsiConsole.Write("> ");
            
            var input = "";
            ConsoleKeyInfo key;
            
            while (true)
            {
                key = Console.ReadKey(true);
                
                if (key.Key == ConsoleKey.Escape)
                {
                    Console.WriteLine();
                    return null; // ESC pressed - cancellation
                }
                else if (key.Key == ConsoleKey.Enter)
                {
                    break;
                }
                else if (key.Key == ConsoleKey.Backspace)
                {
                    if (input.Length > 0)
                    {
                        input = input[..^1];
                        Console.Write("\b \b");
                    }
                }
                else if (!char.IsControl(key.KeyChar))
                {
                    input += key.KeyChar;
                    Console.Write("*"); // Hide password characters
                }
            }
            
            Console.WriteLine();
            input = input.Trim();
            
            // Password cannot be empty
            if (string.IsNullOrWhiteSpace(input))
            {
                AnsiConsole.MarkupLine("[red]Please enter a password.[/]");
                continue;
            }
            
            return input;
        }
    }
}
