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

        table.AddColumn("[rgb(190,140,150)]ID[/]");
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
            var dueDateText = task.DueDate?.ToString("yy/MM/dd") ?? "No due date";
            var projectText = task.Project != null ? $"#{task.Project.Id}: {task.Project.Name}" : "[dim]No project[/]";
            var timeEstimateText = FormatTimeEstimate(task.TimeEstimateMinutes);
            var actualTimeText = TimeTrackingService.FormatActualTime(task);

            table.AddRow(
                task.Id.ToString(),
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
        var projectText = task.Project != null ? $"[rgb(182,196,220)]Project:[/] #{task.Project.Id}: {task.Project.Name}\n" : "";
        var timeTrackingText = $"[rgb(182,196,220)]Time tracking:[/] {TimeTrackingService.GetTimeTrackingStatus(task)}\n";
        var actualTimeText = $"[rgb(182,196,220)]Actual time:[/] {TimeTrackingService.FormatActualTime(task)}\n";

        var panel = new Panel($"[rgb(222,185,149)]{task.Title}[/]\n\n{task.Description}\n\n{assignedToText}{dueDateText}{projectText}{timeTrackingText}{actualTimeText}[rgb(140,140,140)]Created: {task.CreatedOn:yy/MM/dd}[/]")
            .Header($"[rgb(190,140,150)]Task #{task.Id}[/]")
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
            Priority.Urgent => "[rgb(210,120,140)]",     // c.error
            Priority.Important => "[rgb(230,190,140)]",  // c.warning  
            Priority.Want => "[rgb(222,185,149)]",       // c.string
            Priority.Wish => "[rgb(116,143,167)]",       // c.namespace
            _ => "[dim]"
        };
    }

    private static string FormatTimeEstimate(int? timeEstimateMinutes)
    {
        if (!timeEstimateMinutes.HasValue)
            return "[dim]No estimate[/]";

        var totalMinutes = timeEstimateMinutes.Value;
        if (totalMinutes < 60)
            return $"{totalMinutes}min";

        var hours = totalMinutes / 60;
        var remainingMinutes = totalMinutes % 60;

        if (remainingMinutes == 0)
            return $"{hours}h";

        return $"{hours}h {remainingMinutes}m";
    }

    private static string GetStatusColor(WorkStatus status)
    {
        return status switch
        {
            WorkStatus.NotAssigned => "[rgb(140,140,140)]",      // c.comment
            WorkStatus.Assigned => "[rgb(230,190,140)]",         // c.warning
            WorkStatus.Active => "[rgb(222,185,149)]",           // c.string
            WorkStatus.Paused => "[rgb(194,150,107)]",           // c.number
            WorkStatus.Blocked => "[rgb(210,120,140)]",          // c.error
            WorkStatus.Testing => "[rgb(97,160,196)]",           // c.info
            WorkStatus.Finished => "[rgb(182,196,220)]",         // c.property
            _ => "[rgb(140,140,140)]"                            // c.comment
        };
    }
}