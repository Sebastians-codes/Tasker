using Tasker.Domain.Models;

namespace Tasker.Core.Interfaces;

public interface IProjectService
{
    Task<IEnumerable<Project>> GetAllProjectsAsync();
    Task<Project?> GetProjectByIdAsync(int id);
    Task<Project> CreateProjectAsync(string name, string description, Priority priority, DateTimeOffset? dueDate = null);
    Task<Project> UpdateProjectAsync(Project project);
    Task<bool> DeleteProjectAsync(int projectId);
    Task<bool> ProjectExistsAsync(int projectId);
    // Task EnsureSampleDataAsync();
}