using System.Text.Json;
using LogDecoder.Helpers;

namespace LogDecoder.Infrastructure.Configuration;

public sealed class LoggingUserConfig
{
    public string LogDirectory { get; set; } = "logs";
}

public sealed class UserConfig
{
    public LoggingUserConfig Logging { get; set; } = new();

    /// <summary>Folder name of the last-selected protocol config family (pre-selected at startup).</summary>
    public string? ActiveConfig { get; set; }

    public static UserConfig LoadOrCreate()
    {
        var path = GetPath();

        if (!File.Exists(path))
        {
            var config = new UserConfig();
            config.Save();
            return config;
        }

        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<UserConfig>(json, JsonOptions()) ?? new UserConfig();
    }

    public void Save()
    {
        var json = JsonSerializer.Serialize(this, JsonOptions());
        File.WriteAllText(GetPath(), json);
    }

    public static string GetPath()
    {
        return Path.Combine(AppPaths.BaseDirectory, "userconfig.json");
    }

    private static JsonSerializerOptions JsonOptions()
    {
        return new JsonSerializerOptions
        {
            WriteIndented = true
        };
    }
}
