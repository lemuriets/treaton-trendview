using System.Diagnostics;
using LogDecoder.CAN.Packages;
using LogDecoder.CAN.Protocol;
using LogDecoder.Helpers;
using LogDecoder.Infrastructure.Logging;
using LogDecoder.Parser;
using LogDecoder.Parser.Export;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace LogDecoder.CLI;


class Program
{
	public static void Main(string[] args)
    {
        var sw = new Stopwatch();
        sw.Start();
        
        Run();
        
        sw.Stop();
        Console.WriteLine(sw.Elapsed);
    }
    
    private static void Run()
    {
        var ivlLogsFolder = "/Users/lemuriets/Projects/treaton/log decoder/sharp/LogDecoder/test_1";
        var logsFolder = Path.Combine("/Users/lemuriets/Projects/treaton/log decoder/sharp/LogDecoder/test_1", "logs");
        // var logsFolder = "/Volumes/Cucumber/treaton_bin_avl";
        // var logsFolder = "/Volumes/KINGSTON/SD";
        
        LoggerSettingsService.SetLogDirectory(logsFolder);
        using var loggerProvider = new LoggerProvider();
        var logger = loggerProvider.CreateLogger<LogParser>();
        var factory = new CanPackageFactory();
        var parser = new LogParser(logger, ivlLogsFolder, factory);
        
        parser.CreateOrLoadAllIndexes();

        var start = DateTime.Parse("06.08.2025 15:04:00");
        var end = DateTime.Parse("06.08.2025 15:05:08");
        
        var export = new CsvExport(logger, parser);
        export.ToCsv(
            ivlLogsFolder,
            ivlLogsFolder,
            parser.RegisteredIds,
            start,
            end,
            [
                PackageTechStatus.Warning,
                PackageTechStatus.Error,
                PackageTechStatus.Critical,
                PackageTechStatus.Info,
                PackageTechStatus.Ok
            ]);
    }
}
