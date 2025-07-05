using Tasker.Core.Interfaces;
using Tasker.Domain.Models;

namespace Tasker.Cli.Services;

public class TaskService(ITaskRepository taskRepository) : ITaskService
{
    private readonly ITaskRepository _taskRepository = taskRepository;
    private User? _currentUser;

    public void SetCurrentUser(User user)
    {
        _currentUser = user;
    }

    public async Task<IEnumerable<Tasks>> GetAllTasksAsync()
    {
        var allTasks = await _taskRepository.GetAllAsync();
        var tasks = _currentUser != null ? allTasks.Where(t => t.UserId == _currentUser.Id) : allTasks;

        var activeTasks = tasks.Where(t => t.Status == WorkStatus.Active && t.ActiveStartTime.HasValue);
        var hasUpdates = false;

        foreach (var task in activeTasks)
        {
            var currentSessionTime = (int)(DateTimeOffset.Now - task.ActiveStartTime!.Value).TotalMinutes;
            if (currentSessionTime > 0)
            {
                task.ActualTimeMinutes += currentSessionTime;
                task.ActiveStartTime = DateTime.UtcNow;
                await _taskRepository.UpdateAsync(task);
                hasUpdates = true;
            }
        }

        if (hasUpdates)
            await _taskRepository.SaveChangesAsync();

        return tasks.OrderBy(t => t.Priority)
                   .ThenBy(t => t.DueDate ?? DateTimeOffset.MaxValue);
    }

    public async Task<Tasks?> GetTaskByIdAsync(int id)
    {
        var task = await _taskRepository.GetByIdAsync(id);
        if (task != null && _currentUser != null && task.UserId != _currentUser.Id)
            return null; // User can only access their own tasks

        if (task != null)
        {
            if (task.Status == WorkStatus.Active && task.ActiveStartTime.HasValue)
            {
                var currentSessionTime = (int)(DateTimeOffset.Now - task.ActiveStartTime.Value).TotalMinutes;
                if (currentSessionTime > 0)
                {
                    task.ActualTimeMinutes += currentSessionTime;
                    task.ActiveStartTime = DateTime.UtcNow;
                    await _taskRepository.UpdateAsync(task);
                    await _taskRepository.SaveChangesAsync();
                }
            }
        }
        return task;
    }

    public async Task<Tasks> CreateTaskAsync
    (
            string title,
            string description,
            Priority priority,
            DateTime? dueDate = null,
            string? assignedTo = null,
            int? timeEstimateMinutes = null,
            int? projectId = null,
            WorkStatus status =
            WorkStatus.NotAssigned
    )
    {
        if (!string.IsNullOrWhiteSpace(assignedTo) && status == WorkStatus.NotAssigned)
            status = WorkStatus.Assigned;

        var task = new Tasks
        {
            Title = title,
            Description = description,
            Priority = priority,
            DueDate = dueDate,
            AssignedTo = assignedTo,
            TimeEstimateMinutes = timeEstimateMinutes,
            Status = status,
            ProjectId = projectId,
            UserId = _currentUser?.Id ?? throw new InvalidOperationException("Current user not set")
        };

        await _taskRepository.AddAsync(task);
        await _taskRepository.SaveChangesAsync();
        return task;
    }

    public async Task<Tasks> UpdateTaskAsync(Tasks task)
    {
        UpdateStatusBasedOnAssignment(task);

        await _taskRepository.UpdateAsync(task);
        await _taskRepository.SaveChangesAsync();
        return task;
    }

    private static void UpdateStatusBasedOnAssignment(Tasks task)
    {
        if (!string.IsNullOrWhiteSpace(task.AssignedTo) && task.Status == WorkStatus.NotAssigned)
            task.Status = WorkStatus.Assigned;
        else if (string.IsNullOrWhiteSpace(task.AssignedTo) && task.Status == WorkStatus.Assigned)
            task.Status = WorkStatus.NotAssigned;
    }

    public async Task<Tasks> UpdateTaskStatusAsync(int taskId, WorkStatus newStatus)
    {
        var task = await _taskRepository.GetByIdAsync(taskId);
        if (task == null)
            throw new InvalidOperationException($"Task with ID {taskId} not found");

        TimeTrackingService.UpdateTimeTracking(task, newStatus);

        await _taskRepository.UpdateAsync(task);
        await _taskRepository.SaveChangesAsync();
        return task;
    }

    public async Task<bool> CompleteTaskAsync(int taskId)
    {
        var task = await _taskRepository.GetByIdAsync(taskId);
        if (task == null || task.CompletedOn != default)
            return false;

        task.CompletedOn = DateTime.UtcNow;
        task.Status = WorkStatus.Finished;
        task.Priority = Priority.None;
        await _taskRepository.UpdateAsync(task);
        await _taskRepository.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteTaskAsync(int taskId)
    {
        var deleted = await _taskRepository.DeleteAsync(taskId);
        if (deleted)
            await _taskRepository.SaveChangesAsync();

        return deleted;
    }

    public async Task<bool> TaskExistsAsync(int taskId) =>
        await _taskRepository.ExistsAsync(taskId);

    // public async Task EnsureSampleDataAsync()
    // {
    //     var tasks = await _taskRepository.GetAllAsync();
    //     if (tasks.Any())
    //         return;

    //     var sampleTasks = new[]
    //     {
    //         new Tasks
    //         {
    //             Title = "Complete project documentation",
    //             Description = "Write comprehensive documentation for the project",
    //             P?riority = Priority.Important,
    //             DueDate = DateTimeOffset.Now.AddDays(5),
    //             AssignedTo = "John Doe",
    //             TimeEstimateMinutes = 120,
    //             Status = WorkStatus.Active
    //         },
    //         new Tasks
    //         {
    //             Title = "Fix critical bug",
    //             Description = "Resolve issue with data persistence",
    //             Priority = Priority.Urgent,
    //             DueDate = DateTimeOffset.Now.AddDays(1),
    //             AssignedTo = "Jane Smith",
    //             TimeEstimateMinutes = 90,
    //             Status = WorkStatus.Assigned
    //         },
    //         new Tasks
    //         {
    //             Title = "Implement new feature",
    //             Description = "Add user authentication system",
    //             Priority = Priority.Want,
    //             DueDate = DateTimeOffset.Now.AddDays(10),
    //             AssignedTo = "Bob Johnson",
    //             TimeEstimateMinutes = 480,
    //             Status = WorkStatus.Testing
    //         },
    //         new Tasks
    //         {
    //             Title = "Code review",
    //             Description = "Review team's pull requests",
    //             Priority = Priority.Wish,
    //             DueDate = DateTimeOffset.Now.AddDays(3),
    //             TimeEstimateMinutes = 45,
    //             Status = WorkStatus.NotAssigned
    //         }
    //     };

    //     foreach (var task in sampleTasks)
    //         await _taskRepository.AddAsync(task);

    //     await _taskRepository.SaveChangesAsync();
    // }
}
