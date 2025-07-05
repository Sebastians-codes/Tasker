using Tasker.Cli.Helpers;
using Tasker.Infrastructure.Repositories;

namespace Tasker.Cli.UI.Cli;

public class TaskCommands(TaskDisplay taskDisplay, TaskRepository taskRepository, TaskMenu taskMenu)
{
    private readonly TaskDisplay _taskDisplay = taskDisplay;
    private readonly TaskRepository _taskRepository = taskRepository;
    private readonly TaskMenu _taskMenu = taskMenu;

    public async Task Router(string[] args)
    {
        switch (args[0])
        {
            case "tt":
                await PrintAllTasks();
                break;
            case "t":
                if (args.Length < 2)
                    return;
                var arg2 = ArgsParser.ParseSecondArg(args[1]);
                if (!arg2.err)
                    await PrintTask(arg2.arg);
                break;
            case "ta":
                await _taskMenu.AddNewTaskAsync();
                break;
            case "tu":
                await UpdateTask();
                break;
            case "td":
                await DeleteTask();
                break;
            case "tc":
                await CompleteTask();
                break;
        }
    }

    private async Task CompleteTask()
    {
        var tasks = await _taskRepository.GetAllAsync();
        await _taskMenu.CompleteTaskAsync(tasks.ToList());
    }

    private async Task DeleteTask()
    {
        var tasks = await _taskRepository.GetAllAsync();
        await _taskMenu.DeleteTaskAsync(tasks.ToList());
    }

    private async Task UpdateTask()
    {
        var tasks = await _taskRepository.GetAllAsync();
        await _taskMenu.UpdateTaskAsync(tasks.ToList());
    }

    private async Task PrintTask(int id)
    {
        var tasks = await _taskRepository.GetAllAsync();
        var task = tasks.FirstOrDefault(x => x.Id == id);

        if (task is not null)
            _taskDisplay.ShowTaskDetails(task);
        else
            Console.WriteLine("No Task with that id.");
    }

    private async Task PrintAllTasks()
    {
        var tasks = await _taskRepository.GetAllAsync();
        _taskDisplay.ShowTasksTable(tasks);
    }
}