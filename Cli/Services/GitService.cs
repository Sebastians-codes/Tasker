using Tasker.Cli.Helpers;

namespace Tasker.Cli.Services;

public class GitService
{
    public void Push()
    {
        Gitter.Command("add .");
        Gitter.Command("commit -m \"database update\"");
        Gitter.Command("push");
    }
}