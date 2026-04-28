using LogDecoder.Infrastructure.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Serilog;

namespace LogDecoder.Infrastructure.Logging;

public static class LoggerConfig
{
    public static ILoggerFactory CreateLoggerFactory()
    {
        var configuration = LoadConfiguration();
        var logFilePath = CreateLogFilePath();

        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .WriteTo.File(
                path: logFilePath,
                outputTemplate: "[{Level:u3}] ({Timestamp:yyyy-MM-dd HH:mm:ss}) {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        return LoggerFactory.Create(builder =>
        {
            builder.ClearProviders();
            builder.AddSerilog(dispose: true);
        });
    }

    private static IConfigurationRoot LoadConfiguration()
    {
        return new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();
    }

    private static string CreateLogFilePath()
    {
        var userConfig = UserConfig.LoadOrCreate();
        var logsDirectory = Path.IsPathRooted(userConfig.Logging.LogDirectory)
            ? userConfig.Logging.LogDirectory
            : Path.Combine(AppContext.BaseDirectory, userConfig.Logging.LogDirectory);

        Directory.CreateDirectory(logsDirectory);

        var fileName = $"{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.log";

        return Path.Combine(logsDirectory, fileName);

    }
}