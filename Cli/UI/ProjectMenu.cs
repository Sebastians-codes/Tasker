using Spectre.Console;
using Tasker.Core.Interfaces;
using Tasker.Domain.Models;
using Tasker.Cli.Helpers;

namespace Tasker.Cli.UI;

public class ProjectMenu(IProjectService projectService, ProjectDisplay display, TaskMenu taskMenu, ITaskService taskService, TaskDisplay taskDisplay)
{
    private readonly IProjectService _projectService = projectService;
    private readonly ProjectDisplay _display = display;
    private readonly TaskMenu _taskMenu = taskMenu;
    private readonly ITaskService _taskService = taskService;
    private readonly TaskDisplay _taskDisplay = taskDisplay;

    public async Task ShowMenuAsync()
    {
        while (true)
        {
            AnsiConsole.Clear();
            var projects = (await _projectService.GetAllProjectsAsync()).ToList();
            _display.ShowProjectsTable(projects);

            AnsiConsole.WriteLine();

            var action = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .AddChoices([
                        "View project details",
                        "Search by name",
                        "Manage project tasks",
                        "Add new project",
                        "Update project",
                        "Delete project",
                        "Back"
                    ]));

            var shouldContinue = await HandleActionAsync(action, projects);
            if (!shouldContinue)
                break;

            if (action != "Back" && action != "Manage project tasks" && action != "Search by name")
            {
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[dim]Press any key to continue...[/]");
                Console.ReadKey(true);
            }
        }
    }

    private async Task<bool> HandleActionAsync(string action, List<Project> projects)
    {
        switch (action)
        {
            case "View project details":
                await ViewProjectDetailsAsync(projects);
                return true;
            case "Search by name":
                await SearchProjectsByNameAsync();
                return true;
            case "Manage project tasks":
                await ManageProjectTasksAsync(projects);
                return true;
            case "Add new project":
                await AddNewProjectAsync();
                return true;
            case "Update project":
                await UpdateProjectAsync(projects);
                return true;
            case "Delete project":
                await DeleteProjectAsync(projects);
                return true;
            case "Back":
                return false;
            default:
                return true;
        }
    }

    private async Task ViewProjectDetailsAsync(List<Project> projects)
    {
        if (projects.Count == 0)
        {
            _display.ShowErrorMessage("No projects available");
            return;
        }

        var selectedProject = AnsiConsole.Prompt(
            new SelectionPrompt<Project>()
                .Title("Select a project to view:")
                .UseConverter(project => $"{project.Id}: {project.Name}")
                .AddChoices(projects));

        var fullProject = await _projectService.GetProjectByIdAsync(selectedProject.Id);
        if (fullProject != null)
        {
            AnsiConsole.Clear();
            _display.ShowProjectDetails(fullProject);
        }
    }

    public async Task AddNewProjectAsync()
    {
        var name = AnsiConsole.Ask<string>("Enter project name:");
        var description = AnsiConsole.Ask<string>("Enter project description:");
        var priority = AnsiConsole.Prompt(
            new SelectionPrompt<Priority>()
                .Title("Select priority:")
                .AddChoices(Enum.GetValues<Priority>()));

        await _projectService.CreateProjectAsync(name, description, priority);
        _display.ShowSuccessMessage($"Project '{name}' added successfully!");
    }

    public async Task UpdateProjectAsync(List<Project> projects)
    {
        if (projects.Count == 0)
        {
            _display.ShowErrorMessage("No projects available");
            return;
        }

        var projectToUpdate = AnsiConsole.Prompt(
            new SelectionPrompt<Project>()
                .Title("Select a project to update:")
                .UseConverter(project => $"{project.Id}: {project.Name}")
                .AddChoices(projects));

        var fieldToUpdate = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("What would you like to update?")
                .AddChoices([
                    "Name",
                    "Description",
                    "Priority"
                ]));

        switch (fieldToUpdate)
        {
            case "Name":
                projectToUpdate.Name = AnsiConsole.Ask<string>("Enter new name:", projectToUpdate.Name);
                break;
            case "Description":
                try
                {
                    _display.ShowMessage("Opening description in external editor...");
                    var editedDescription = await TextEditor.EditTextAsync(projectToUpdate.Description);
                    projectToUpdate.Description = editedDescription.Trim();
                }
                catch (Exception ex)
                {
                    _display.ShowErrorMessage($"Failed to open editor: {ex.Message}");
                    _display.ShowMessage("Falling back to simple input...");
                    projectToUpdate.Description = AnsiConsole.Ask<string>("Enter new description:", projectToUpdate.Description);
                }
                break;
            case "Priority":
                projectToUpdate.Priority = AnsiConsole.Prompt(
                    new SelectionPrompt<Priority>()
                        .Title("Select new priority:")
                        .AddChoices(Enum.GetValues<Priority>()));
                break;
        }

        await _projectService.UpdateProjectAsync(projectToUpdate);
        _display.ShowSuccessMessage($"Project '{projectToUpdate.Name}' updated successfully!");
    }

    public async Task DeleteProjectAsync(List<Project> projects)
    {
        if (projects.Count == 0)
        {
            _display.ShowErrorMessage("No projects available");
            return;
        }

        var projectToDelete = AnsiConsole.Prompt(
            new SelectionPrompt<Project>()
                .Title("Select a project to delete:")
                .UseConverter(project => $"{project.Id}: {project.Name}")
                .AddChoices(projects));

        if (AnsiConsole.Confirm($"Are you sure you want to delete '{projectToDelete.Name}'?"))
        {
            await _projectService.DeleteProjectAsync(projectToDelete.Id);
            _display.ShowErrorMessage($"Project '{projectToDelete.Name}' deleted!");
        }
    }

    private async Task ManageProjectTasksAsync(List<Project> projects)
    {
        if (projects.Count == 0)
        {
            _display.ShowErrorMessage("No projects available");
            return;
        }

        var selectedProject = AnsiConsole.Prompt(
            new SelectionPrompt<Project>()
                .Title("Select a project to manage tasks:")
                .UseConverter(project => $"{project.Id}: {project.Name}")
                .AddChoices(projects));

        await ShowProjectTaskMenuAsync(selectedProject);
    }

    private async Task ShowProjectTaskMenuAsync(Project project)
    {
        while (true)
        {
            AnsiConsole.Clear();
            
            var allTasks = await _taskService.GetAllTasksAsync();
            var projectTasks = allTasks.Where(t => t.ProjectId == project.Id).ToList();
            
            AnsiConsole.MarkupLine($"[bold green]Project: {project.Name}[/]");
            AnsiConsole.WriteLine();
            
            if (projectTasks.Count > 0)
            {
                _taskDisplay.ShowTasksTable(projectTasks);
            }
            else
            {
                AnsiConsole.MarkupLine("[dim]No tasks in this project[/]");
            }
            
            AnsiConsole.WriteLine();

            var action = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("What would you like to do?")
                    .AddChoices([
                        "View task details",
                        "Add new task to project",
                        "Update task",
                        "Delete task",
                        "Mark task as complete",
                        "Back"
                    ]));

            var shouldContinue = await HandleProjectTaskActionAsync(action, project, projectTasks);
            if (!shouldContinue)
                break;

            if (action != "Back")
            {
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[dim]Press any key to continue...[/]");
                Console.ReadKey(true);
            }
        }
    }

    private async Task<bool> HandleProjectTaskActionAsync(string action, Project project, List<Tasks> projectTasks)
    {
        switch (action)
        {
            case "View task details":
                await ViewProjectTaskDetailsAsync(projectTasks);
                return true;
            case "Add new task to project":
                await AddTaskToProjectAsync(project);
                return true;
            case "Update task":
                await _taskMenu.UpdateTaskAsync(projectTasks);
                return true;
            case "Delete task":
                await _taskMenu.DeleteTaskAsync(projectTasks);
                return true;
            case "Mark task as complete":
                await _taskMenu.CompleteTaskAsync(projectTasks);
                return true;
            case "Back":
                return false;
            default:
                return true;
        }
    }

    private async Task ViewProjectTaskDetailsAsync(List<Tasks> projectTasks)
    {
        if (projectTasks.Count == 0)
        {
            _display.ShowErrorMessage("No tasks in this project");
            return;
        }

        var selectedTask = AnsiConsole.Prompt(
            new SelectionPrompt<Tasks>()
                .Title("Select a task to view:")
                .UseConverter(task => $"{task.Id}: {task.Title}")
                .AddChoices(projectTasks));

        AnsiConsole.Clear();
        _taskDisplay.ShowTaskDetails(selectedTask);
    }

    private async Task AddTaskToProjectAsync(Project project)
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

        await _taskService.CreateTaskAsync(title, description, priority, dueDate, assignedTo, timeEstimate, project.Id);
        _display.ShowSuccessMessage($"Task '{title}' added to project '{project.Name}' successfully!");
    }

    private async Task SearchProjectsByNameAsync()
    {
        var searchTerm = AnsiConsole.Ask<string>("Enter project name to search for:");
        
        var allProjects = (await _projectService.GetAllProjectsAsync()).ToList();
        var matchingProjects = allProjects.Where(p => 
            p.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)).ToList();
        
        AnsiConsole.Clear();
        AnsiConsole.MarkupLine($"[bold green]Projects matching '{searchTerm}':[/]");
        AnsiConsole.WriteLine();
        
        if (matchingProjects.Count > 0)
        {
            _display.ShowProjectsTable(matchingProjects);
            AnsiConsole.WriteLine();
            
            var action = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("What would you like to do with these results?")
                    .AddChoices([
                        "View project details",
                        "Manage project tasks", 
                        "Update project",
                        "Delete project",
                        "Back"
                    ]));

            if (action != "Back")
            {
                await HandleActionAsync(action, matchingProjects);
            }
        }
        else
        {
            AnsiConsole.MarkupLine($"[dim]No projects found matching '{searchTerm}'[/]");
        }
    }

}
