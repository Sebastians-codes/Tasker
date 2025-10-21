using Domain.Models;

namespace Tasker.Core.Interfaces;

public interface IProjectService
{
    void SetCurrentUser(User user);
    Task<IEnumerable<Project>> GetAllProjectsAsync();
    Task<Project?> GetProjectByIdAsync(Guid id);
    Task<Project> CreateProjectAsync(string name, string description, Priority priority);
    Task<Project> UpdateProjectAsync(Project project);
    Task<bool> DeleteProjectAsync(Guid projectId);
    Task<bool> ProjectExistsAsync(Guid projectId);
    Task<bool> ProjectNameExistsAsync(string name);
}
