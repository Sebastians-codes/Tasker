using Tasker.Domain.Models;

namespace Tasker.Core.Interfaces;

public interface ITaskRepository
{
    Task<IEnumerable<Tasks>> GetAllAsync();
    Task<Tasks?> GetByIdAsync(Guid id);
    Task<Tasks> AddAsync(Tasks task);
    Task<Tasks> UpdateAsync(Tasks task);
    Task<bool> DeleteAsync(Guid id);
    Task<bool> ExistsAsync(Guid id);
    Task<bool> TaskNameExistsAsync(string title, Guid userId, Guid? projectId);
    Task<int> SaveChangesAsync();
}