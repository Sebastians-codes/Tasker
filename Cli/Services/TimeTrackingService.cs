using Tasker.Cli.UI;
using Domain.Models;

namespace Tasker.Cli.Services;

public static class TimeTrackingService
{
    public static void UpdateTimeTracking(Tasks task, WorkStatus newStatus)
    {
        var now = DateTime.UtcNow;
        var currentStatus = task.Status;

        if (currentStatus == WorkStatus.Active && task.ActiveStartTime.HasValue)
        {
            var activeTime = (int)(now - task.ActiveStartTime.Value).TotalMinutes;
            task.ActualTimeMinutes += activeTime;
        }

        switch (newStatus)
        {
            case WorkStatus.Active:
                task.ActiveStartTime = now;
                task.LastPausedTime = null;
                break;

            case WorkStatus.Paused:
            case WorkStatus.Blocked:
            case WorkStatus.Testing:
                if (currentStatus == WorkStatus.Active && task.ActiveStartTime.HasValue)
                {
                    task.LastPausedTime = now;
                    task.ActiveStartTime = null;
                }
                break;

            case WorkStatus.Finished:
                task.ActiveStartTime = null;
                task.LastPausedTime = null;
                break;

            case WorkStatus.NotAssigned:
            case WorkStatus.Assigned:
                if (currentStatus == WorkStatus.Active)
                {
                    task.ActiveStartTime = null;
                }
                break;
        }

        task.Status = newStatus;
    }

    public static int GetCurrentActualTime(Tasks task) =>
        task.ActualTimeMinutes;

    public static string FormatActualTime(Tasks task)
    {
        var totalMinutes = GetCurrentActualTime(task);

        if (totalMinutes == 0)
            return "[dim]No time tracked[/]";

        return TaskDisplay.FormatTimeMinutes(totalMinutes);
    }

    public static string GetTimeTrackingStatus(Tasks task)
    {
        return task.Status switch
        {
            WorkStatus.Active when task.ActiveStartTime.HasValue => "[green]⏱ Running[/]",
            WorkStatus.Paused => "[yellow]⏸ Paused[/]",
            WorkStatus.Blocked => "[red]⏸ Paused (Blocked)[/]",
            WorkStatus.Testing => "[blue]⏸ Paused (Testing)[/]",
            WorkStatus.Finished => "[dim green]✓ Complete[/]",
            _ => "[dim]⏹ Not tracking[/]"
        };
    }
}
