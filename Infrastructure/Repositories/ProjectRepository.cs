using Tasker.Core.Interfaces;
using Tasker.Domain.Models;
using Tasker.Infrastructure.Data;

namespace Tasker.Infrastructure.Repositories;

public class ProjectRepository(DatabaseManager databaseManager) : IProjectRepository
{
    private readonly DatabaseManager _databaseManager = databaseManager;

    public async Task<IEnumerable<Project>> GetAllAsync() =>
        await _databaseManager.GetAllProjectsWithTasksAsync();

    public async Task<Project?> GetByIdAsync(int id) =>
        await _databaseManager.GetProjectWithTasksAsync(id);

    public async Task<Project> AddAsync(Project project)
    {
        return await _databaseManager.AddAsync(project);
    }

    public async Task<Project> UpdateAsync(Project project)
    {
        project.UpdatedOn = DateTime.UtcNow;
        return await _databaseManager.UpdateAsync(project);
    }

    public async Task<bool> DeleteAsync(int id)
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

    public async Task<bool> ExistsAsync(int id)
    {
        var project = await _databaseManager.GetAsync<Project>(id);
        return project != null;
    }

    public async Task<int> SaveChangesAsync()
    {
        // DatabaseManager handles saves automatically
        return await Task.FromResult(1);
    }
}