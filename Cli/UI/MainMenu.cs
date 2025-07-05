using Spectre.Console;

namespace Tasker.Cli.UI;

public class MainMenu(TaskMenu taskMenu, ProjectMenu projectMenu)
{
    private readonly TaskMenu _taskMenu = taskMenu;
    private readonly ProjectMenu _projectMenu = projectMenu;

    public async Task ShowMenuAsync()
    {
        while (true)
        {
            AnsiConsole.Clear();
            AnsiConsole.Write(new FigletText("Tasker").Centered().Color(Color.Orange1));
            AnsiConsole.WriteLine();

            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .AddChoices([
                        "Tasks",
                        "Projects",
                        "Exit"
                    ]));

            switch (choice)
            {
                case "Tasks":
                    await _taskMenu.ShowMenuAsync();
                    break;
                case "Projects":
                    await _projectMenu.ShowMenuAsync();
                    break;
                case "Exit":
                    return;
            }
        }
    }
}