using Tasker.Core.Interfaces;
using Tasker.Domain.Models;
using Tasker.Infrastructure.Data;

namespace Tasker.Infrastructure.Repositories;

public class TaskRepository(DatabaseManager databaseManager) : ITaskRepository
{
    private readonly DatabaseManager _databaseManager = databaseManager;

    public async Task<IEnumerable<Tasks>> GetAllAsync()
    {
        var tasks = await _databaseManager.GetAllTasksWithProjectAsync();
        
        foreach (var task in tasks)
        {
            task.Decrypt();
        }
        
        return tasks;
    }

    public async Task<Tasks?> GetByIdAsync(Guid id)
    {
        var task = await _databaseManager.GetTaskWithProjectAsync(id);
        
        task?.Decrypt();
        
        return task;
    }

    public async Task<Tasks> AddAsync(Tasks task)
    {
        task.Encrypt();
        
        var result = await _databaseManager.AddAsync(task);
        
        result.Decrypt();
        
        return result;
    }

    public async Task<Tasks> UpdateAsync(Tasks task)
    {
        task.UpdatedOn = DateTime.UtcNow;
        
        task.Encrypt();
        
        var result = await _databaseManager.UpdateAsync(task);
        
        result.Decrypt();
        
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
