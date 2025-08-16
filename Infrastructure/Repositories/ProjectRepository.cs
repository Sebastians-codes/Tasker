using Tasker.Core.Interfaces;
using Tasker.Domain.Models;
using Tasker.Infrastructure.Data;

namespace Tasker.Infrastructure.Repositories;

public class ProjectRepository(DatabaseManager databaseManager) : IProjectRepository
{
    private readonly DatabaseManager _databaseManager = databaseManager;

    public async Task<IEnumerable<Project>> GetAllAsync()
    {
        var projects = await _databaseManager.GetAllProjectsWithTasksAsync();

        return projects;
    }

    public async Task<Project?> GetByIdAsync(Guid id)
    {
        var project = await _databaseManager.GetProjectWithTasksAsync(id);

        return project;
    }

    public async Task<Project> AddAsync(Project project)
    {
        var result = await _databaseManager.AddAsync(project);

        return result;
    }

    public async Task<Project> UpdateAsync(Project project)
    {
        project.UpdatedOn = DateTime.UtcNow;

        var result = await _databaseManager.UpdateAsync(project);

        return result;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        try
        {
            await _databaseManager.DeleteAsync<Project>(id);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        var project = await _databaseManager.GetAsync<Project>(id);
        return project != null;
    }

    public async Task<bool> ProjectNameExistsAsync(string name, Guid userId)
    {
        return await _databaseManager.ProjectNameExistsAsync(name, userId);
    }

    public async Task<int> SaveChangesAsync()
    {
        return await Task.FromResult(1);
    }
}
