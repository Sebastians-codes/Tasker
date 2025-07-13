using Tasker.Domain.Models;

namespace Tasker.Core.Interfaces;

public interface ITaskService
{
    void SetCurrentUser(User user);
    Task<IEnumerable<Tasks>> GetAllTasksAsync();
    Task<Tasks?> GetTaskByIdAsync(Guid id);
    Task<Tasks> CreateTaskAsync(string title, string description, Priority priority, DateTime? dueDate = null, string? assignedTo = null, int? timeEstimateMinutes = null, Guid? projectId = null, WorkStatus status = WorkStatus.NotAssigned);
    Task<Tasks> UpdateTaskAsync(Tasks task);
    Task<Tasks> UpdateTaskStatusAsync(Guid taskId, WorkStatus newStatus);
    Task<bool> CompleteTaskAsync(Guid taskId);
    Task<bool> DeleteTaskAsync(Guid taskId);
    Task<bool> TaskExistsAsync(Guid taskId);
    Task<bool> TaskNameExistsAsync(string title, Guid? projectId);
}