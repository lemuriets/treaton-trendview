using System.Collections;
using System.Collections.ObjectModel;
using System.Diagnostics;
using LogDecoder.CAN.General;
using LogDecoder.CAN.Packages;
using LogDecoder.Helpers;
using LogDecoder.Helpers.TimeHelper;
using LogDecoder.Parser;
using LogDecoder.Parser.Export;
using OxyPlot;

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
        var logsFolder = "/Users/lemuriets/Projects/treaton/log decoder/sharp/LogDecoder/test_full";
        
        // win
        // var logsFolder = "C:\\Users\\madyarov\\projects\\AVL-log-decoder\\test_1";
        
        
        var parser = new LogParser(logsFolder);
        parser.CreateAndLoadAllIndexes();

        var export = new ExcelExport(parser);
        export.ToExcel($"{logsFolder}/00", logsFolder, [0x401, 1120], DateTime.Parse("28.07.2025 10:13:18"), DateTime.Parse("07.08.2025 14:41:46"));
    }
}
