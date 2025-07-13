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

    public async Task<Project?> GetProjectByIdAsync(Guid id)
    {
        var project = await _projectRepository.GetByIdAsync(id);
        if (project != null && _currentUser != null && project.OwnerId != _currentUser.Id)
            return null;
        return project;
    }

    public async Task<Project> CreateProjectAsync(string name, string description, Priority priority)
    {
        var currentUserId = _currentUser?.Id ?? throw new InvalidOperationException("Current user not set");

        var project = new Project
        {
            Name = name,
            Description = description,
            Priority = priority,
            OwnerId = currentUserId
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

    public async Task<bool> DeleteProjectAsync(Guid projectId)
    {
        var deleted = await _projectRepository.DeleteAsync(projectId);
        if (deleted)
            await _projectRepository.SaveChangesAsync();
        return deleted;
    }

    public async Task<bool> ProjectExistsAsync(Guid projectId) =>
        await _projectRepository.ExistsAsync(projectId);

    public async Task<bool> ProjectNameExistsAsync(string name)
    {
        var currentUserId = _currentUser?.Id ?? throw new InvalidOperationException("Current user not set");
        return await _projectRepository.ProjectNameExistsAsync(name, currentUserId);
    }

}