using Tasker.Core.Interfaces;
using Tasker.Domain.Models;
using Tasker.Infrastructure.Data;

namespace Tasker.Infrastructure.Repositories;

public class TaskRepository(DatabaseManager databaseManager) : ITaskRepository
{
    private readonly DatabaseManager _databaseManager = databaseManager;

    public async Task<IEnumerable<Tasks>> GetAllAsync() =>
        await _databaseManager.GetAllTasksWithProjectAsync();

    public async Task<Tasks?> GetByIdAsync(int id) =>
        await _databaseManager.GetTaskWithProjectAsync(id);

    public async Task<Tasks> AddAsync(Tasks task)
    {
        return await _databaseManager.AddAsync(task);
    }

    public async Task<Tasks> UpdateAsync(Tasks task)
    {
        task.UpdatedOn = DateTime.UtcNow;
        return await _databaseManager.UpdateAsync(task);
    }

    public async Task<bool> DeleteAsync(int id)
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

    public async Task<bool> ExistsAsync(int id)
    {
        var task = await _databaseManager.GetAsync<Tasks>(id);
        return task != null;
    }

    public async Task<int> SaveChangesAsync()
    {
        // DatabaseManager handles saves automatically
        return await Task.FromResult(1);
    }
}
