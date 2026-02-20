using System.Collections;
using System.Collections.ObjectModel;
using System.Diagnostics;
using LogDecoder.CAN.Contracts;
using LogDecoder.CAN.General;
using LogDecoder.CAN.Packages;
using LogDecoder.Helpers;
using LogDecoder.Helpers.TimeHelper;
using LogDecoder.Parser;
using LogDecoder.Parser.Export;
using OfficeOpenXml;
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
        // var logsFolder = "/Users/lemuriets/Projects/treaton/log decoder/sharp/LogDecoder/test_1";
        var logsFolder = "/Volumes/Cucumber/treaton_bin_avl";
        
        // win
        // var logsFolder = "C:\\Users\\madyarov\\projects\\AVL-log-decoder\\test_1";
        
        
        var parser = new LogParser(logsFolder);
        parser.CreateOrLoadAllIndexes();

        var start = DateTime.Parse("28.07.2025 10:17:55");
        var end = DateTime.Parse("28.07.2025 18:45:30");

        // ICanPackageParsed last = null;
        // foreach (var p in parser.GetPackages([1120], start, end))
        // {
        //     last = p;
        // }
        // Console.WriteLine(last.ParseData().Value.Messages[0]);
        
        var export = new ExcelExport(parser);
        export.ToExcel($"{logsFolder}/00", logsFolder, parser.IdsAll, start, end);
    }
}
