using Domain.Models;
using Tasker.Core.Interfaces;
using Tasker.Infrastructure.Data;

namespace Tasker.Infrastructure.Repositories;

public class TaskRepository(DatabaseManager databaseManager) : ITaskRepository
{
    private readonly DatabaseManager _databaseManager = databaseManager;

    public async Task<IEnumerable<Tasks>> GetAllAsync()
    {
        var tasks = await _databaseManager.GetAllTasksWithProjectAsync();

        return tasks;
    }

    public async Task<Tasks?> GetByIdAsync(Guid id)
    {
        var task = await _databaseManager.GetTaskWithProjectAsync(id);

        return task;
    }

    public async Task<Tasks> AddAsync(Tasks task)
    {
        var result = await _databaseManager.AddAsync(task);
        return result;
    }

    public async Task<Tasks> UpdateAsync(Tasks task)
    {
        task.UpdatedOn = DateTime.UtcNow;

        var result = await _databaseManager.UpdateAsync(task);

        return result;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        try
        {
            await _databaseManager.DeleteAsync<Tasks>(id);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        var task = await _databaseManager.GetAsync<Tasks>(id);
        return task != null;
    }

    public async Task<bool> TaskNameExistsAsync(string title, Guid userId, Guid? projectId)
    {
        return await _databaseManager.TaskNameExistsAsync(title, userId, projectId);
    }

    public async Task<int> SaveChangesAsync()
    {
        return await Task.FromResult(1);
    }
}
