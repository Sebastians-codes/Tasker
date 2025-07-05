using Tasker.Cli.Helpers;
using Tasker.Infrastructure.Repositories;

namespace Tasker.Cli.UI.Cli;

public class ProjectCommands(ProjectDisplay projectDisplay, ProjectRepository projectRepository, ProjectMenu projectMenu)
{
    private readonly ProjectDisplay _projectDisplay = projectDisplay;
    private readonly ProjectRepository _projectRepository = projectRepository;
    private readonly ProjectMenu _projectMenu = projectMenu;

    public async Task Router(string[] args)
    {
        switch (args[0])
        {
            case "pp":
                await PrintAllProjects();
                break;
            case "p":
                if (args.Length < 2)
                    return;
                var arg2 = ArgsParser.ParseSecondArg(args[1]);
                if (!arg2.err)
                    await PrintProject(arg2.arg);
                break;
            case "pa":
                await _projectMenu.AddNewProjectAsync();
                break;
            case "pu":
                await UpdateProject();
                break;
            case "pd":
                await DeleteProject();
                break;
        }
    }

    private async Task DeleteProject()
    {
        var projects = await _projectRepository.GetAllAsync();
        await _projectMenu.DeleteProjectAsync(projects.ToList());
    }

    private async Task UpdateProject()
    {
        var projects = await _projectRepository.GetAllAsync();
        await _projectMenu.UpdateProjectAsync(projects.ToList());
    }

    private async Task PrintProject(int id)
    {
        var project = await _projectRepository.GetByIdAsync(id);

        if (project is not null)
            _projectDisplay.ShowProjectDetails(project);
        else
            Console.WriteLine("No Project with that id");
    }

    private async Task PrintAllProjects()
    {
        var projects = await _projectRepository.GetAllAsync();
        _projectDisplay.ShowProjectsTable(projects);
    }
}