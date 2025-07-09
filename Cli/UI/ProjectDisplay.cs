using Spectre.Console;
using Tasker.Domain.Models;

namespace Tasker.Cli.UI;

public class ProjectDisplay(TaskDisplay taskDisplay)
{
    private readonly TaskDisplay _taskDisplay = taskDisplay;

    public void ShowProjectsTable(IEnumerable<Project> projects)
    {
        var table = new Table
        {
            Title = new TableTitle("[rgb(222,185,149)]Projects[/]"),
            Border = TableBorder.Rounded
        };
        table.Centered();

        table.AddColumn("[rgb(190,140,150)]Name[/]");
        table.AddColumn("[rgb(190,140,150)]Priority[/]");
        table.AddColumn("[rgb(190,140,150)]Task Count[/]");
        table.AddColumn("[rgb(190,140,150)]Total Estimate[/]");
        table.AddColumn("[rgb(190,140,150)]Total Actual[/]");

        foreach (var project in projects)
        {
            var priorityColor = TaskDisplay.GetPriorityColor(project.Priority);
            var taskCount = project.Tasks?.Count ?? 0;
            var taskCountText = taskCount > 0 ? taskCount.ToString() : "[dim]0[/]";

            // Calculate cumulative time estimates and actual time
            var totalEstimateMinutes = project.Tasks?.Where(t => !t.IsDeleted).Sum(t => t.TimeEstimateMinutes ?? 0) ?? 0;
            var totalActualMinutes = project.Tasks?.Where(t => !t.IsDeleted).Sum(t => t.ActualTimeMinutes) ?? 0;
            
            var estimateText = totalEstimateMinutes > 0 
                ? TaskDisplay.FormatTimeMinutes(totalEstimateMinutes)
                : "[dim]No estimates[/]";
                
            var actualText = totalActualMinutes > 0 
                ? TaskDisplay.FormatTimeMinutes(totalActualMinutes)
                : "[dim]No time tracked[/]";

            table.AddRow(
                project.Name,
                $"{priorityColor}{project.Priority}[/]",
                taskCountText,
                estimateText,
                actualText
            );
        }

        AnsiConsole.Write(table);
    }

    public void ShowProjectDetails(Project project)
    {
        var taskCount = project.Tasks?.Count ?? 0;
        var taskCountText = $"[rgb(182,196,220)]Tasks:[/] {taskCount}\n";

        // Calculate cumulative time estimates and actual time
        var totalEstimateMinutes = project.Tasks?.Where(t => !t.IsDeleted).Sum(t => t.TimeEstimateMinutes ?? 0) ?? 0;
        var totalActualMinutes = project.Tasks?.Where(t => !t.IsDeleted).Sum(t => t.ActualTimeMinutes) ?? 0;
        
        var estimateText = totalEstimateMinutes > 0 
            ? $"[rgb(182,196,220)]Total Estimate:[/] {TaskDisplay.FormatTimeMinutes(totalEstimateMinutes)}\n"
            : "[rgb(182,196,220)]Total Estimate:[/] [dim]No estimates[/]\n";
            
        var actualText = totalActualMinutes > 0 
            ? $"[rgb(182,196,220)]Total Actual:[/] {TaskDisplay.FormatTimeMinutes(totalActualMinutes)}\n"
            : "[rgb(182,196,220)]Total Actual:[/] [dim]No time tracked[/]\n";

        var panel = new Panel($"[rgb(222,185,149)]{project.Name}[/]\n\n{project.Description}\n\n{taskCountText}{estimateText}{actualText}[rgb(140,140,140)]Created: {project.CreatedOn:yyyy/MM/dd}[/]")
            .Header($"[rgb(190,140,150)]Project Details[/]")
            .Border(BoxBorder.Rounded)
            .Padding(2, 1);

        AnsiConsole.Write(Align.Center(panel));

        if (project.Tasks != null && project.Tasks.Count > 0)
        {
            AnsiConsole.WriteLine();
            _taskDisplay.ShowTasksTable(project.Tasks);
        }
        else
        {
            AnsiConsole.WriteLine();
            AnsiConsole.Write(Align.Center(new Text("No tasks assigned to this project yet.")));
            AnsiConsole.WriteLine();
        }
    }

    public void ShowMessage(string message, string color = "white") =>
        _taskDisplay.ShowMessage(message, color);

    public void ShowSuccessMessage(string message) =>
        _taskDisplay.ShowSuccessMessage(message);

    public void ShowErrorMessage(string message) =>
        _taskDisplay.ShowErrorMessage(message);

    public void ShowInfoMessage(string message) =>
        _taskDisplay.ShowInfoMessage(message);

}