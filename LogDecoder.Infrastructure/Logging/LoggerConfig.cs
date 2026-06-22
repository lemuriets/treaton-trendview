using LogDecoder.Infrastructure.Configuration;
using LogDecoder.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;

namespace LogDecoder.Infrastructure.Logging;

public static class LoggerConfig
{
    private const string OutputTemplate =
        "[{Level:u3}] ({Timestamp:yyyy-MM-dd HH:mm:ss}) {Message:lj}{NewLine}{Exception}";
    
    public static ILoggerFactory CreateLoggerFactory()
    {
        // Intentionally not merging appsettings.json into the logger: a stale config next to the
        // exe could carry a Serilog File sink with "path": "logs/.log", which (rollingInterval
        // Infinite) writes that path verbatim and produces a nameless ".log" file alongside ours.
        // Sinks are configured from code below. LoadConfiguration() is kept for future use.
        // var configuration = LoadConfiguration();
        var logFilePath = CreateLogFilePath();
        
#if DEBUG
        const LogEventLevel minimumLevel = LogEventLevel.Debug;
#else
        const LogEventLevel minimumLevel = LogEventLevel.Warning;
#endif

        var loggerConfig = new LoggerConfiguration()
            .MinimumLevel.Is(minimumLevel)
            // .ReadFrom.Configuration(configuration)
            .WriteTo.Console(outputTemplate: OutputTemplate)
            .WriteTo.Sink(InMemoryLogSink.Instance);
            
#if !DEBUG
        loggerConfig
            .WriteTo.File(
                path: logFilePath,
                rollingInterval: RollingInterval.Infinite,
                outputTemplate: OutputTemplate);
#endif
        Log.Logger = loggerConfig.CreateLogger();

        return LoggerFactory.Create(builder =>
        {
            builder.ClearProviders();
            builder.AddSerilog(dispose: true);
        });
    }

    private static IConfigurationRoot LoadConfiguration()
    {
        return new ConfigurationBuilder()
            .SetBasePath(AppPaths.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .Build();
    }

    private static string CreateLogFilePath()
    {
        var userConfig = UserConfig.LoadOrCreate();
        var logsDirectory = Path.IsPathRooted(userConfig.Logging.LogDirectory)
            ? userConfig.Logging.LogDirectory
            : Path.Combine(AppPaths.BaseDirectory, userConfig.Logging.LogDirectory);

        Directory.CreateDirectory(logsDirectory);

        var fileName = $"{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.log";

        return Path.Combine(logsDirectory, fileName);

    }
}