using Spectre.Console;
using Tasker.Domain.Models;
using Tasker.Cli.Services;

namespace Tasker.Cli.UI;

public class TaskDisplay
{
    public void ShowTasksTable(IEnumerable<Tasks> tasks)
    {
        var table = new Table
        {
            Title = new TableTitle("[rgb(222,185,149)]Tasks[/]"),
            Border = TableBorder.Rounded
        };
        table.Centered();

        table.AddColumn("[rgb(190,140,150)]Title[/]");
        table.AddColumn("[rgb(190,140,150)]Priority[/]");
        table.AddColumn("[rgb(190,140,150)]Status[/]");
        table.AddColumn("[rgb(190,140,150)]Due Date[/]");
        table.AddColumn("[rgb(190,140,150)]Project[/]");
        table.AddColumn("[rgb(190,140,150)]Assigned To[/]");
        table.AddColumn("[rgb(190,140,150)]Estimate[/]");
        table.AddColumn("[rgb(190,140,150)]Actual[/]");

        foreach (var task in tasks)
        {
            var priorityColor = GetPriorityColor(task.Priority);
            var statusColor = GetStatusColor(task.Status);
            var dueDateText = task.DueDate?.ToString("yyyy/MM/dd") ?? "No due date";
            var projectText = task.Project != null ? task.Project.Name : "[dim]No project[/]";
            var timeEstimateText = FormatTimeEstimate(task.TimeEstimateMinutes);
            var actualTimeText = TimeTrackingService.FormatActualTime(task);

            table.AddRow(
                task.Title,
                $"{priorityColor}{task.Priority}[/]",
                $"{statusColor}{task.Status}[/]",
                dueDateText,
                projectText,
                task.AssignedTo ?? "[dim]Unassigned[/]",
                timeEstimateText,
                actualTimeText
            );
        }

        AnsiConsole.Write(table);
    }

    public void ShowTaskDetails(Tasks task)
    {
        var assignedToText = !string.IsNullOrEmpty(task.AssignedTo) ? $"[rgb(182,196,220)]Assigned to:[/] {task.AssignedTo}\n" : "";
        var dueDateText = task.DueDate.HasValue ? $"[rgb(182,196,220)]Due:[/] {task.DueDate:yyyy/MM/dd}\n" : "";
        var projectText = task.Project != null ? $"[rgb(182,196,220)]Project:[/] {task.Project.Name}\n" : "";
        var timeTrackingText = $"[rgb(182,196,220)]Time tracking:[/] {TimeTrackingService.GetTimeTrackingStatus(task)}\n";
        var actualTimeText = $"[rgb(182,196,220)]Actual time:[/] {TimeTrackingService.FormatActualTime(task)}\n";

        var panel = new Panel($"[rgb(222,185,149)]{task.Title}[/]\n\n{task.Description}\n\n{assignedToText}{dueDateText}{projectText}{timeTrackingText}{actualTimeText}[rgb(140,140,140)]Created: {task.CreatedOn:yyyy/MM/dd}[/]")
            .Header($"[rgb(190,140,150)]Task Details[/]")
            .Border(BoxBorder.Rounded)
            .Padding(2, 1);

        AnsiConsole.Write(Align.Center(panel));
    }

    public void ShowMessage(string message, string color = "white") =>
        AnsiConsole.MarkupLine($"[{color}]{message}[/]");

    public void ShowSuccessMessage(string message) =>
        ShowMessage(message, "green");

    public void ShowErrorMessage(string message) =>
        ShowMessage(message, "red");

    public void ShowInfoMessage(string message) =>
        ShowMessage(message, "blue");

    public static string GetPriorityColor(Priority priority)
    {
        return priority switch
        {
            Priority.Urgent => "[rgb(210,120,140)]",
            Priority.Important => "[rgb(230,190,140)]",
            Priority.Want => "[rgb(222,185,149)]",
            Priority.Wish => "[rgb(116,143,167)]",
            _ => "[dim]"
        };
    }

    public static string FormatTimeMinutes(int totalMinutes)
    {
        if (totalMinutes == 0)
            return "0min";
            
        if (totalMinutes < 60)
            return $"{totalMinutes}min";

        var parts = new List<string>();
        
        // Calculate years (assuming 365 days per year, 8 hours per day)
        var minutesPerYear = 365 * 24 * 60;
        if (totalMinutes >= minutesPerYear)
        {
            var years = totalMinutes / minutesPerYear;
            parts.Add($"{years}y");
            totalMinutes %= minutesPerYear;
        }
        
        // Calculate months (assuming 30 days per month, 8 hours per day)
        var minutesPerMonth = 30 * 24 * 60;
        if (totalMinutes >= minutesPerMonth)
        {
            var months = totalMinutes / minutesPerMonth;
            parts.Add($"{months}mo");
            totalMinutes %= minutesPerMonth;
        }
        
        // Calculate weeks
        var minutesPerWeek = 7 * 24 * 60;
        if (totalMinutes >= minutesPerWeek)
        {
            var weeks = totalMinutes / minutesPerWeek;
            parts.Add($"{weeks}w");
            totalMinutes %= minutesPerWeek;
        }
        
        // Calculate days
        var minutesPerDay = 24 * 60;
        if (totalMinutes >= minutesPerDay)
        {
            var days = totalMinutes / minutesPerDay;
            parts.Add($"{days}d");
            totalMinutes %= minutesPerDay;
        }
        
        // Calculate hours
        if (totalMinutes >= 60)
        {
            var hours = totalMinutes / 60;
            parts.Add($"{hours}h");
            totalMinutes %= 60;
        }
        
        // Calculate remaining minutes
        if (totalMinutes > 0)
        {
            parts.Add($"{totalMinutes}m");
        }
        
        return string.Join(" ", parts);
    }

    private static string FormatTimeEstimate(int? timeEstimateMinutes)
    {
        if (!timeEstimateMinutes.HasValue)
            return "[dim]No estimate[/]";

        return FormatTimeMinutes(timeEstimateMinutes.Value);
    }

    private static string GetStatusColor(WorkStatus status)
    {
        return status switch
        {
            WorkStatus.NotAssigned => "[rgb(140,140,140)]",
            WorkStatus.Assigned => "[rgb(230,190,140)]",
            WorkStatus.Active => "[rgb(222,185,149)]",
            WorkStatus.Paused => "[rgb(194,150,107)]",
            WorkStatus.Blocked => "[rgb(210,120,140)]",
            WorkStatus.Testing => "[rgb(97,160,196)]",
            WorkStatus.Finished => "[rgb(182,196,220)]",
            _ => "[rgb(140,140,140)]"
        };
    }
}