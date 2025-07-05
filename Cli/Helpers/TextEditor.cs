using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Tasker.Cli.Helpers;

public static class TextEditor
{
    public static async Task<string> EditTextAsync(string initialText)
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"tasker_edit_{Guid.NewGuid():N}.tmp");

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
                    Arguments = BuildSecureArguments(editorCommand.Value.Arguments, tempFile),
                    UseShellExecute = false
                }
            };

            process.Start();
            await process.WaitForExitAsync();

            if (!File.Exists(tempFile))
                throw new InvalidOperationException("Temp file was deleted during editing");

            return await File.ReadAllTextAsync(tempFile);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                try
                {
                    var random = new Random();
                    var fileSize = new FileInfo(tempFile).Length;
                    var overwriteData = new byte[fileSize];
                    random.NextBytes(overwriteData);
                    await File.WriteAllBytesAsync(tempFile, overwriteData);
                    File.Delete(tempFile);
                }
                catch
                {
                    File.Delete(tempFile);
                }
            }
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

    private static string BuildSecureArguments(string baseArguments, string tempFile)
    {
        var escapedPath = EscapeShellArgument(tempFile);

        if (string.IsNullOrEmpty(baseArguments))
            return escapedPath;

        return $"{baseArguments} {escapedPath}";
    }

    private static string EscapeShellArgument(string argument)
    {
        if (string.IsNullOrEmpty(argument))
            return "\"\"";

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ||
            RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return $"\"{argument.Replace("\"", "\\\"")}\"";
        else
            return $"\"{argument.Replace("\"", "\\\"")}\"";
    }
}
