using System.Collections;
using System.Collections.ObjectModel;
using System.Diagnostics;
using LogDecoder.CAN.Contracts;
using LogDecoder.CAN.General;
using LogDecoder.CAN.Packages;
using LogDecoder.Helpers;
using LogDecoder.Helpers.TimeHelper;
using LogDecoder.Parser;
using LogDecoder.Parser.Data;
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
        var logsFolder = "/Users/lemuriets/Projects/treaton/log decoder/sharp/LogDecoder/test_1";
        // var logsFolder = "/Volumes/Cucumber/treaton_bin_avl";
        
        // win
        // var logsFolder = "C:\\Users\\madyarov\\Desktop\\sd\\a";



        var factory = new CanPackageFactory();
        var parser = new LogParser(logsFolder, factory);
        parser.CreateOrLoadAllIndexes();

        var start = DateTime.Parse("06.11.2025 08:17:55");
        var end = DateTime.Parse("08.11.2025 18:45:30");
        
        var export = new ExcelExport(parser);
        // export.ToExcel(logsFolder, logsFolder, parser.RegisteredIds, start, end);
    }
}
