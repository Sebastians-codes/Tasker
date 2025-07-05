using Spectre.Console;
using Tasker.Core.Interfaces;
using Tasker.Domain.Models;
using Tasker.Cli.Helpers;

namespace Tasker.Cli.UI;

public class ProjectMenu(IProjectService projectService, ProjectDisplay display)
{
    private readonly IProjectService _projectService = projectService;
    private readonly ProjectDisplay _display = display;

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
                        "Add new project",
                        "Update project",
                        "Delete project",
                        "Back to main menu"
                    ]));

            var shouldContinue = await HandleActionAsync(action, projects);
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

    private async Task<bool> HandleActionAsync(string action, List<Project> projects)
    {
        switch (action)
        {
            case "View project details":
                await ViewProjectDetailsAsync(projects);
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
            case "Back to main menu":
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

        DateTime? dueDate = null;
        if (AnsiConsole.Confirm("Set due date?"))
        {
            var dateInput = AnsiConsole.Ask<string>("Enter due date (MM/dd or yyyy-MM-dd):");
            dueDate = InputParser.ParseDate(dateInput);
            if (dueDate == null)
                _display.ShowErrorMessage("Invalid date format. Project will be created without due date.");
        }

        await _projectService.CreateProjectAsync(name, description, priority, dueDate);
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
                    "Priority",
                    "Due Date"
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
            case "Due Date":
                if (AnsiConsole.Confirm("Set due date?"))
                {
                    var dateInput = AnsiConsole.Ask<string>("Enter due date (MM/dd or yyyy-MM-dd):");
                    var parsedDate = InputParser.ParseDate(dateInput);
                    if (parsedDate != null)
                        projectToUpdate.DueDate = parsedDate;
                    else
                        _display.ShowErrorMessage("Invalid date format. Due date not updated.");
                }
                else
                    projectToUpdate.DueDate = null;
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
}
