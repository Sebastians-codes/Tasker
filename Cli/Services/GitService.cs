using Tasker.Cli.Helpers;

namespace Tasker.Cli.Services;

public class GitService
{
    public string Push() =>
        Gitter.Command("push");
}