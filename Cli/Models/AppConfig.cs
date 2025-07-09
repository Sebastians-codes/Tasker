using System.Text.Json;

namespace Tasker.Cli.Models;

public class AppConfig
{
    public string? SessionToken { get; set; }
    public string? EncryptedConnectionString { get; set; }

    private static readonly string ConfigFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Tasker",
        "config.json");

    public static AppConfig Load()
    {
        try
        {
            if (File.Exists(ConfigFilePath))
            {
                var json = File.ReadAllText(ConfigFilePath);
                var config = JsonSerializer.Deserialize<AppConfig>(json);
                return config ?? new AppConfig();
            }
        }
        catch
        {
        }

        return new AppConfig();
    }

    public void Save()
    {
        try
        {
            var directory = Path.GetDirectoryName(ConfigFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(ConfigFilePath, json);
        }
        catch
        {
        }
    }
}