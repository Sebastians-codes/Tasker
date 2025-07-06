using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace Tasker.Cli.Services;

public static class MachineIdService
{
    public static string GetMachineId()
    {
        try
        {
            // Collect multiple identifiers for stronger fingerprinting
            var identifiers = new List<string>();
            
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                identifiers.Add(GetWindowsMachineId());
                identifiers.Add(Environment.MachineName);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                identifiers.Add(GetLinuxMachineId());
                identifiers.Add(Environment.MachineName);
                identifiers.Add(GetLinuxSystemId());
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                identifiers.Add(GetMacMachineId());
                identifiers.Add(Environment.MachineName);
            }
            else
            {
                identifiers.Add(Environment.MachineName);
                identifiers.Add(Environment.UserName);
            }
            
            // Remove empty identifiers and combine
            var validIdentifiers = identifiers.Where(x => !string.IsNullOrWhiteSpace(x));
            var combinedInfo = string.Join(":", validIdentifiers);
            
            // Add system entropy (but stable) and use SHA-512 for stronger collision resistance
            var entropyString = $"{Environment.ProcessorCount}:{Environment.Is64BitOperatingSystem}:{Environment.OSVersion.Platform}";
            var finalInput = $"{combinedInfo}:{entropyString}";
            
            using var sha512 = SHA512.Create();
            var hash = sha512.ComputeHash(Encoding.UTF8.GetBytes(finalInput));
            return Convert.ToHexString(hash);
        }
        catch
        {
            // Secure fallback - use multiple system properties with entropy
            var fallback = $"{Environment.MachineName}:{Environment.UserName}:{Environment.OSVersion}:{Environment.ProcessorCount}";
            using var sha512 = SHA512.Create();
            var hash = sha512.ComputeHash(Encoding.UTF8.GetBytes(fallback));
            return Convert.ToHexString(hash);
        }
    }

    private static string GetWindowsMachineId()
    {
        try
        {
            // Use wmic command as fallback since System.Management isn't available on all platforms
            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "wmic",
                    Arguments = "csproduct get uuid /value",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };
            
            process.Start();
            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            
            // Parse UUID from output
            var lines = output.Split('\n');
            foreach (var line in lines)
            {
                if (line.StartsWith("UUID="))
                {
                    var uuid = line.Split('=')[1].Trim();
                    if (!string.IsNullOrWhiteSpace(uuid))
                        return uuid;
                }
            }
            
            return Environment.MachineName;
        }
        catch
        {
            return Environment.MachineName;
        }
    }

    private static string GetLinuxMachineId()
    {
        try
        {
            // Try /etc/machine-id first (systemd)
            if (File.Exists("/etc/machine-id"))
            {
                return File.ReadAllText("/etc/machine-id").Trim();
            }
            
            // Try /var/lib/dbus/machine-id (older systems)
            if (File.Exists("/var/lib/dbus/machine-id"))
            {
                return File.ReadAllText("/var/lib/dbus/machine-id").Trim();
            }
            
            // Fallback to hostname
            return Environment.MachineName;
        }
        catch
        {
            return Environment.MachineName;
        }
    }

    private static string GetMacMachineId()
    {
        try
        {
            // Use system_profiler to get hardware UUID
            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "system_profiler",
                    Arguments = "SPHardwareDataType",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };
            
            process.Start();
            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            
            // Parse hardware UUID from output
            var lines = output.Split('\n');
            foreach (var line in lines)
            {
                if (line.Contains("Hardware UUID:"))
                {
                    var uuid = line.Split(':')[1].Trim();
                    return uuid;
                }
            }
            
            return Environment.MachineName;
        }
        catch
        {
            return Environment.MachineName;
        }
    }

    private static string GetLinuxSystemId()
    {
        try
        {
            // Try to get additional Linux system identifiers
            var identifiers = new List<string>();
            
            // CPU info
            if (File.Exists("/proc/cpuinfo"))
            {
                var cpuInfo = File.ReadAllText("/proc/cpuinfo");
                var serialLine = cpuInfo.Split('\n').FirstOrDefault(line => line.StartsWith("Serial"));
                if (!string.IsNullOrEmpty(serialLine))
                {
                    identifiers.Add(serialLine.Split(':')[1].Trim());
                }
            }
            
            // Boot ID
            if (File.Exists("/proc/sys/kernel/random/boot_id"))
            {
                identifiers.Add(File.ReadAllText("/proc/sys/kernel/random/boot_id").Trim());
            }
            
            return string.Join(":", identifiers.Where(x => !string.IsNullOrWhiteSpace(x)));
        }
        catch
        {
            return string.Empty;
        }
    }
}