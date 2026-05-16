using System.Buffers;
using System.Diagnostics;
using LogDecoder.CAN.Packages;
using LogDecoder.CAN.Protocol;
using LogDecoder.Infrastructure.Logging;
using LogDecoder.Parser;
using LogDecoder.Parser.Export;

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
        var ivlLogsFolder = "/Users/lemuriets/Projects/treaton/log decoder/sharp/LogDecoder/problem_log_test";
        var logsFolder = Path.Combine(ivlLogsFolder, "logs");
        // var logsFolder = "/Volumes/Cucumber/treaton_bin_avl";
        // var logsFolder = "/Volumes/KINGSTON/SD";
        
        LoggerSettingsService.SetLogDirectory(logsFolder);
        using var loggerProvider = new LoggerProvider();
        var logger = loggerProvider.CreateLogger<LogParser>();
        var factory = new CanPackageFactory();
        var parser = new LogParser(logger, ivlLogsFolder, factory);
        
        parser.CreateOrLoadAllIndexes();

        var start = DateTime.Parse("16.10.2025 17:36:27");
        var end = DateTime.Parse("05.02.2026 11:58:10");

        // var start = parser.MinDatetime.Value;
        // var end = parser.MaxDatetime.Value;
        // IReadOnlySet<int> ids = parser.RegisteredIds;
        IReadOnlySet<int> ids = new HashSet<int>(){IdSynchro.Id, IdModeCivl.Id};
        
        var export = new CsvExport(logger, parser);
        export.ToCsv(
            ivlLogsFolder,
            ivlLogsFolder,
            ids,
            start,
            end,
            new HashSet<PackageTechStatus> {
                PackageTechStatus.Warning,
                PackageTechStatus.Error,
                PackageTechStatus.Critical,
                PackageTechStatus.Info,
                PackageTechStatus.Ok
            },
            true,
            false);
    }
}
