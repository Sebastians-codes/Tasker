using Tasker.Core.Interfaces;
using Tasker.Domain.Models;

namespace Tasker.Cli.Services;

public class ProjectService(IProjectRepository projectRepository) : IProjectService
{
    private readonly IProjectRepository _projectRepository = projectRepository;
    private User? _currentUser;

    public void SetCurrentUser(User user)
    {
        _currentUser = user;
    }

    public async Task<IEnumerable<Project>> GetAllProjectsAsync()
    {
        var allProjects = await _projectRepository.GetAllAsync();
        var projects = _currentUser != null ? allProjects.Where(p => p.OwnerId == _currentUser.Id) : allProjects;
        return projects.OrderBy(p => p.Priority)
                      .ThenBy(p => p.CreatedOn);
    }

    public async Task<Project?> GetProjectByIdAsync(int id)
    {
        var project = await _projectRepository.GetByIdAsync(id);
        if (project != null && _currentUser != null && project.OwnerId != _currentUser.Id)
            return null; // User can only access their own projects
        return project;
    }

    public async Task<Project> CreateProjectAsync(string name, string description, Priority priority)
    {
        var project = new Project
        {
            Name = name,
            Description = description,
            Priority = priority,
            OwnerId = _currentUser?.Id ?? throw new InvalidOperationException("Current user not set")
        };

        await _projectRepository.AddAsync(project);
        await _projectRepository.SaveChangesAsync();
        return project;
    }

    public async Task<Project> UpdateProjectAsync(Project project)
    {
        await _projectRepository.UpdateAsync(project);
        await _projectRepository.SaveChangesAsync();
        return project;
    }

    public async Task<bool> DeleteProjectAsync(int projectId)
    {
        var deleted = await _projectRepository.DeleteAsync(projectId);
        if (deleted)
            await _projectRepository.SaveChangesAsync();
        return deleted;
    }

    public async Task<bool> ProjectExistsAsync(int projectId) =>
        await _projectRepository.ExistsAsync(projectId);

    // public async Task EnsureSampleDataAsync()
    // {
    //     var projects = await _projectRepository.GetAllAsync();
    //     if (projects.Any())
    //         return;

    //     var sampleProjects = new[]
    //     {
    //         new Project
    //         {
    //             Name = "Website Redesign",
    //             Description = "Complete overhaul of company website with modern design and improved UX",
    //             Priority = Priority.Important,
    //             DueDate = DateTimeOffset.Now.AddDays(30)
    //         },
    //         new Project
    //         {
    //             Name = "Mobile App Development",
    //             Description = "Create mobile application for iOS and Android platforms",
    //             Priority = Priority.Want,
    //             DueDate = DateTimeOffset.Now.AddDays(60)
    //         },
    //         new Project
    //         {
    //             Name = "Database Migration",
    //             Description = "Migrate legacy database to new cloud infrastructure",
    //             Priority = Priority.Urgent,
    //             DueDate = DateTimeOffset.Now.AddDays(15)
    //         }
    //     };

    //     foreach (var project in sampleProjects)
    //         await _projectRepository.AddAsync(project);

    //     await _projectRepository.SaveChangesAsync();
    // }
}