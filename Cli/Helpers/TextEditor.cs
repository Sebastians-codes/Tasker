using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Tasker.Cli.Helpers;

public static class TextEditor
{
    public static async Task<string> EditTextAsync(string initialText)
    {
        var tempFile = Path.GetTempFileName();

        try
        {
            await File.WriteAllTextAsync(tempFile, initialText);

            var editorCommand = GetEditorCommand();
            if (editorCommand == null)
                throw new InvalidOperationException("No suitable text editor found");

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = editorCommand.Value.FileName,
                    Arguments = $"{editorCommand.Value.Arguments} \"{tempFile}\"",
                    UseShellExecute = false
                }
            };

            process.Start();
            await process.WaitForExitAsync();

            return await File.ReadAllTextAsync(tempFile);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    private static (string FileName, string Arguments)? GetEditorCommand()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            if (IsCommandAvailable("code"))
                return ("code", "--wait");

            return ("notepad", "");
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ||
                 RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            if (IsCommandAvailable("nvim"))
                return ("nvim", "");
            if (IsCommandAvailable("vim"))
                return ("vim", "");
            if (IsCommandAvailable("nano"))
                return ("nano", "");

            return ("vi", "");
        }

        return null;
    }

    private static bool IsCommandAvailable(string command)
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "where" : "which",
                    Arguments = command,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            process.Start();
            process.WaitForExit();

            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }
}
