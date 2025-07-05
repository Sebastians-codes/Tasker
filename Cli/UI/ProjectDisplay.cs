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

        table.AddColumn("[rgb(190,140,150)]ID[/]");
        table.AddColumn("[rgb(190,140,150)]Name[/]");
        table.AddColumn("[rgb(190,140,150)]Priority[/]");
        table.AddColumn("[rgb(190,140,150)]Task Count[/]");

        foreach (var project in projects)
        {
            var priorityColor = TaskDisplay.GetPriorityColor(project.Priority);
            var taskCount = project.Tasks?.Count ?? 0;
            var taskCountText = taskCount > 0 ? taskCount.ToString() : "[dim]0[/]";

            table.AddRow(
                project.Id.ToString(),
                project.Name,
                $"{priorityColor}{project.Priority}[/]",
                taskCountText
            );
        }

        AnsiConsole.Write(table);
    }

    public void ShowProjectDetails(Project project)
    {
        var taskCount = project.Tasks?.Count ?? 0;
        var taskCountText = $"[rgb(182,196,220)]Tasks:[/] {taskCount}\n";

        var panel = new Panel($"[rgb(222,185,149)]{project.Name}[/]\n\n{project.Description}\n\n{taskCountText}[rgb(140,140,140)]Created: {project.CreatedOn:yy/MM/dd}[/]")
            .Header($"[rgb(190,140,150)]Project #{project.Id}[/]")
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