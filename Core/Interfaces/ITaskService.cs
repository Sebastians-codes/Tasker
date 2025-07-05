using Tasker.Domain.Models;

namespace Tasker.Core.Interfaces;

public interface ITaskService
{
    void SetCurrentUser(User user);
    Task<IEnumerable<Tasks>> GetAllTasksAsync();
    Task<Tasks?> GetTaskByIdAsync(int id);
    Task<Tasks> CreateTaskAsync(string title, string description, Priority priority, DateTimeOffset? dueDate = null, string? assignedTo = null, int? timeEstimateMinutes = null, int? projectId = null, WorkStatus status = WorkStatus.NotAssigned);
    Task<Tasks> UpdateTaskAsync(Tasks task);
    Task<Tasks> UpdateTaskStatusAsync(int taskId, WorkStatus newStatus);
    Task<bool> CompleteTaskAsync(int taskId);
    Task<bool> DeleteTaskAsync(int taskId);
    Task<bool> TaskExistsAsync(int taskId);
    // Task EnsureSampleDataAsync();
}