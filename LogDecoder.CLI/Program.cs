using System.Diagnostics;
using LogDecoder.CAN.Packages;
using LogDecoder.Parser;

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
        // mac
        // var logsFolder = "/Users/lemuriets/Projects/treaton/log decoder/sharp/LogDecoder/test_1";
        // var logsFolder = "/Volumes/Cucumber/treaton_bin_avl";
        var logsFolder = "/Volumes/KINGSTON/SD";
        
        // win
        



        var factory = new CanPackageFactory();
        var parser = new LogParser(logsFolder, factory);
        parser.CreateOrLoadAllIndexes();

        // var start = DateTime.Parse("18.11.2025 02:11:27");
        // var end = DateTime.Parse("18.11.2025 02:11:26");
        //
        // var export = new ExcelExport(parser);
        // export.ToExcel(logsFolder, logsFolder, parser.RegisteredIds, start, end);
    }
}
