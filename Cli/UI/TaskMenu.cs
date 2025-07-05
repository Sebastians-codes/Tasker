using Spectre.Console;
using Tasker.Core.Interfaces;
using Tasker.Domain.Models;
using Tasker.Cli.Helpers;

namespace Tasker.Cli.UI;

public class TaskMenu(ITaskService taskService, IProjectService projectService, TaskDisplay display)
{
    private readonly ITaskService _taskService = taskService;
    private readonly IProjectService _projectService = projectService;
    private readonly TaskDisplay _display = display;

    public async Task ShowMenuAsync()
    {
        while (true)
        {
            AnsiConsole.Clear();
            var tasks = (await _taskService.GetAllTasksAsync()).ToList();
            _display.ShowTasksTable(tasks);

            AnsiConsole.WriteLine();

            var action = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .AddChoices([
                        "View task details",
                        "Mark task as complete",
                        "Add new task",
                        "Update task",
                        "Delete task",
                        "Back to main menu"
                    ]));

            var shouldContinue = await HandleActionAsync(action, tasks);
            if (!shouldContinue)
                break;

            if (action != "Back to main menu")
            {
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[dim]Press any key to continue...[/]");
                Console.ReadKey(true);
            }
        }
    }

    private async Task<bool> HandleActionAsync(string action, List<Tasks> tasks)
    {
        switch (action)
        {
            case "View task details":
                await ViewTaskDetailsAsync(tasks);
                return true;
            case "Mark task as complete":
                await CompleteTaskAsync(tasks);
                return true;
            case "Add new task":
                await AddNewTaskAsync();
                return true;
            case "Update task":
                await UpdateTaskAsync(tasks);
                return true;
            case "Delete task":
                await DeleteTaskAsync(tasks);
                return true;
            case "Back to main menu":
                return false;
            default:
                return true;
        }
    }

    private async Task ViewTaskDetailsAsync(List<Tasks> tasks)
    {
        if (tasks.Count == 0)
        {
            _display.ShowErrorMessage("No tasks available");
            return;
        }

        var selectedTask = AnsiConsole.Prompt(
            new SelectionPrompt<Tasks>()
                .Title("Select a task to view:")
                .UseConverter(task => $"{task.Id}: {task.Title}")
                .AddChoices(tasks));

        AnsiConsole.Clear();
        _display.ShowTaskDetails(selectedTask);
    }

    public async Task CompleteTaskAsync(List<Tasks> tasks)
    {
        var incompleteTasks = tasks.Where(t => t.CompletedOn == default).ToList();
        if (incompleteTasks.Count == 0)
        {
            _display.ShowErrorMessage("No incomplete tasks available");
            return;
        }

        var taskToComplete = AnsiConsole.Prompt(
            new SelectionPrompt<Tasks>()
                .Title("Select a task to mark as complete:")
                .UseConverter(task => $"{task.Id}: {task.Title}")
                .AddChoices(incompleteTasks));

        await _taskService.CompleteTaskAsync(taskToComplete.Id);
        _display.ShowSuccessMessage($"Task '{taskToComplete.Title}' marked as complete!");
    }

    public async Task AddNewTaskAsync()
    {
        var title = AnsiConsole.Ask<string>("Enter task title:");
        var description = AnsiConsole.Ask<string>("Enter task description:");
        var priority = AnsiConsole.Prompt(
            new SelectionPrompt<Priority>()
                .Title("Select priority:")
                .AddChoices(Enum.GetValues<Priority>()));

        DateTime? dueDate = null;
        if (AnsiConsole.Confirm("Set due date?"))
        {
            var dateInput = AnsiConsole.Ask<string>("Enter due date (MM/dd or yyyy-MM-dd):");
            dueDate = InputParser.ParseDate(dateInput);
            if (dueDate == null)
                _display.ShowErrorMessage("Invalid date format. Task will be created without due date.");
        }

        var assignedTo = AnsiConsole.Confirm("Assign to someone?")
            ? AnsiConsole.Ask<string>("Enter assignee name:")
            : null;

        int? timeEstimate = null;
        if (AnsiConsole.Confirm("Set time estimate?"))
        {
            var timeInput = AnsiConsole.Ask<string>("Enter time estimate (e.g., 1h 30m, 1.5h, 90):");
            timeEstimate = InputParser.ParseTimeEstimate(timeInput);
            if (timeEstimate == null)
                _display.ShowErrorMessage("Invalid time format. Task will be created without time estimate.");
        }

        await _taskService.CreateTaskAsync(title, description, priority, dueDate, assignedTo, timeEstimate);
        _display.ShowSuccessMessage($"Task '{title}' added successfully!");
    }

    public async Task UpdateTaskAsync(List<Tasks> tasks)
    {
        if (!tasks.Any())
        {
            _display.ShowErrorMessage("No tasks available");
            return;
        }

        var taskToUpdate = AnsiConsole.Prompt(
            new SelectionPrompt<Tasks>()
                .Title("Select a task to update:")
                .UseConverter(task => $"{task.Id}: {task.Title}")
                .AddChoices(tasks));

        var fieldToUpdate = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("What would you like to update?")
                .AddChoices([
                    "Title",
                    "Description",
                    "Priority",
                    "Status",
                    "Due Date",
                    "Project",
                    "Assigned To",
                    "Time Estimate"
                ]));

        switch (fieldToUpdate)
        {
            case "Title":
                taskToUpdate.Title = AnsiConsole.Ask<string>("Enter new title:", taskToUpdate.Title);
                break;
            case "Description":
                try
                {
                    _display.ShowMessage("Opening description in external editor...");
                    var editedDescription = await TextEditor.EditTextAsync(taskToUpdate.Description);
                    taskToUpdate.Description = editedDescription.Trim();
                }
                catch (Exception ex)
                {
                    _display.ShowErrorMessage($"Failed to open editor: {ex.Message}");
                    _display.ShowMessage("Falling back to simple input...");
                    taskToUpdate.Description = AnsiConsole.Ask<string>("Enter new description:", taskToUpdate.Description);
                }
                break;
            case "Priority":
                taskToUpdate.Priority = AnsiConsole.Prompt(
                    new SelectionPrompt<Priority>()
                        .Title("Select new priority:")
                        .AddChoices(Enum.GetValues<Priority>()));
                break;
            case "Status":
                var newStatus = AnsiConsole.Prompt(
                    new SelectionPrompt<WorkStatus>()
                        .Title("Select new status:")
                        .AddChoices(Enum.GetValues<WorkStatus>()));

                await _taskService.UpdateTaskStatusAsync(taskToUpdate.Id, newStatus);
                _display.ShowSuccessMessage($"Status updated to {newStatus} with time tracking!");
                return;
            case "Due Date":
                if (AnsiConsole.Confirm("Set due date?"))
                {
                    var dateInput = AnsiConsole.Ask<string>("Enter due date (MM/dd or yyyy-MM-dd):");
                    var parsedDate = InputParser.ParseDate(dateInput);
                    if (parsedDate != null)
                        taskToUpdate.DueDate = parsedDate;
                    else
                        _display.ShowErrorMessage("Invalid date format. Due date not updated.");
                }
                else
                    taskToUpdate.DueDate = null;
                break;
            case "Project":
                var projects = (await _projectService.GetAllProjectsAsync()).ToList();

                var projectChoices = new List<string> { "No project" };
                projectChoices.AddRange(projects.Select(p => $"#{p.Id}: {p.Name}"));

                var selectedProject = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("Select project:")
                        .AddChoices(projectChoices));

                if (selectedProject == "No project")
                {
                    taskToUpdate.ProjectId = null;
                    taskToUpdate.Project = null;
                }
                else
                {
                    var projectId = int.Parse(selectedProject.Split(':')[0].TrimStart('#'));
                    taskToUpdate.ProjectId = projectId;
                    taskToUpdate.Project = projects.First(p => p.Id == projectId);
                }
                break;
            case "Assigned To":
                var wasAssigned = !string.IsNullOrWhiteSpace(taskToUpdate.AssignedTo);
                taskToUpdate.AssignedTo = AnsiConsole.Confirm("Assign to someone?")
                    ? AnsiConsole.Ask<string>("Enter assignee name:", taskToUpdate.AssignedTo ?? "")
                    : null;

                var isNowAssigned = !string.IsNullOrWhiteSpace(taskToUpdate.AssignedTo);

                if (!wasAssigned && isNowAssigned && taskToUpdate.Status == WorkStatus.NotAssigned)
                    _display.ShowMessage("Status will be automatically changed to 'Assigned'", "yellow");
                else if (wasAssigned && !isNowAssigned && taskToUpdate.Status == WorkStatus.Assigned)
                    _display.ShowMessage("Status will be automatically changed to 'NotAssigned'", "yellow");
                break;
            case "Time Estimate":
                if (AnsiConsole.Confirm("Set time estimate?"))
                {
                    var currentEstimate = taskToUpdate.TimeEstimateMinutes?.ToString() ?? "";
                    var timeInput = AnsiConsole.Ask<string>("Enter time estimate (e.g., 1h 30m, 1.5h, 90):", currentEstimate);
                    var parsedTime = InputParser.ParseTimeEstimate(timeInput);
                    if (parsedTime != null)
                        taskToUpdate.TimeEstimateMinutes = parsedTime;
                    else
                        _display.ShowErrorMessage("Invalid time format. Time estimate not updated.");
                }
                else
                    taskToUpdate.TimeEstimateMinutes = null;
                break;
        }

        await _taskService.UpdateTaskAsync(taskToUpdate);
        _display.ShowSuccessMessage($"Task '{taskToUpdate.Title}' updated successfully!");
    }

    public async Task DeleteTaskAsync(List<Tasks> tasks)
    {
        if (tasks.Count == 0)
        {
            _display.ShowErrorMessage("No tasks available");
            return;
        }

        var taskToDelete = AnsiConsole.Prompt(
            new SelectionPrompt<Tasks>()
                .Title("Select a task to delete:")
                .UseConverter(task => $"{task.Id}: {task.Title}")
                .AddChoices(tasks));

        if (AnsiConsole.Confirm($"Are you sure you want to delete '{taskToDelete.Title}'?"))
        {
            await _taskService.DeleteTaskAsync(taskToDelete.Id);
            _display.ShowErrorMessage($"Task '{taskToDelete.Title}' deleted!");
        }
    }
}