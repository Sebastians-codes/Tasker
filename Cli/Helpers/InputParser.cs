using System.Globalization;
using System.Text.RegularExpressions;

namespace Tasker.Cli.Helpers;

public static class InputParser
{
    public static DateTime? ParseDate(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;

        // Try parsing MM/dd format
        if (Regex.IsMatch(input, @"^\d{1,2}/\d{1,2}$"))
        {
            var parts = input.Split('/');
            if (parts.Length == 2 &&
                int.TryParse(parts[0], out int month) &&
                int.TryParse(parts[1], out int day))
            {
                var currentYear = DateTime.UtcNow.Year;
                try
                {
                    var date = DateTime.SpecifyKind(new DateTime(currentYear, month, day), DateTimeKind.Utc);

                    // If the date has already passed this year, use next year
                    if (date < DateTime.Today)
                        date = DateTime.SpecifyKind(new DateTime(currentYear + 1, month, day), DateTimeKind.Utc);

                    return date;
                }
                catch (ArgumentOutOfRangeException)
                {
                    return null; // Invalid date
                }
            }
        }

        // Try parsing full date formats (yyyy/MM/dd, yyyy-MM-dd, etc.)
        if (DateTime.TryParse(input, out DateTime result))
        {
            // Validate that the year is not in the past and not too far in the future
            var currentYear = DateTime.UtcNow.Year;
            if (result.Year < currentYear)
            {
                return null; // Reject dates with past years
            }
            
            // Optional: Prevent dates too far in the future (e.g., beyond 10 years)
            if (result.Year > currentYear + 10)
            {
                return null; // Reject dates beyond reasonable future
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

            // Parse hours
            if (match.Groups[1].Success && double.TryParse(match.Groups[1].Value, out double hours))
                totalMinutes += (int)(hours * 60);

            // Parse minutes
            if (match.Groups[2].Success && int.TryParse(match.Groups[2].Value, out int minutes))
                totalMinutes += minutes;

            return totalMinutes > 0 ? totalMinutes : null;
        }

        if (int.TryParse(input, out int plainMinutes))
            return plainMinutes;

        return null;
    }
}
