using System.Diagnostics;

namespace Tasker.Cli.Helpers;

public class Gitter
{
    public static string Command(string args)
    {
        var processInfo = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = args,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = "~/source/csharp/Tasker"
        };

        using var process = Process.Start(processInfo);
        if (process is null)
            throw new Exception("Gitter could not run");

        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();

        process.WaitForExit();

        if (process.ExitCode != 0)
            throw new Exception($"Git command failed {error}");

        return output;
    }
}