using LogDecoder.Infrastructure.Configuration;

namespace LogDecoder.Infrastructure.Logging;

public static class LoggerSettingsService
{
    public static string GetLogDirectory()
    {
        var config = UserConfig.LoadOrCreate();

        return config.Logging.LogDirectory;
    }

    public static void SetLogDirectory(string logDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(logDirectory);

        Directory.CreateDirectory(logDirectory);

        var config = UserConfig.LoadOrCreate();
        config.Logging.LogDirectory = logDirectory;
        config.Save();
    }
}