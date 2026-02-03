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
        parser.CreateAllIndexes();

        var export = new ExportService(parser);
        export.ToExcel($"{logsFolder}/00", logsFolder, [0x401, 1120], DateTime.Parse("28.07.2025 10:13:18"), DateTime.Parse("07.08.2025 14:41:46"));

        // var dp = new DataProvider(parser);
        //
        // var start = DateTime.Parse("28.07.2025 10:13:11");
        //
        // var allSeries = new TrendsData();
        // var messages = new ObservableCollection<LogMessage>();
        // dp.GetDataForTimeSpan(allSeries, messages, start, 300, 0);
    }
}


public class DataProvider(LogParser parser)
{
    public void GetDataForTimeSpan(TrendsData seriesPool, ObservableCollection<LogMessage> messages, DateTime start, int lengthSec, double startX)
    {
        var x = startX;
        var end = start.AddSeconds(lengthSec);
        var packages = parser.GetPackagesForTimeSpan(parser.IdsAll, start, end);
        if (packages.Count == 0)
        {
            return;
        }
        Console.WriteLine($"{packages.Count} {start} - {lengthSec}");
        var prevHrc = packages[0].Hrc;
        foreach (var pkg in packages)
        {
            Console.WriteLine(x);
            var hrcDelta = CanUtils.CalcHrcDelta(prevHrc, pkg.Hrc);
            var shift = hrcDelta / TimeHelper.MicrosecondsPerSecond;
            
            x += shift;
            prevHrc = pkg.Hrc;

            var parsedData = pkg.ParseData();
            if (parsedData is null)
            {
                continue;
            }
            
            foreach (var msg in parsedData.Value.Messages)
            {
                messages.Add(new LogMessage(msg, pkg.TechStatus));
            }

            foreach (var (name, value) in parsedData.Value.NumericData)
            {
                var series = seriesPool.GetSeriesByName(name);
                series?.Add(x, value);
            }
        }
    }
}

public class LogMessage(string text, PackageTechStatus status)
{
    public string Text { get; set; } = text;
    public PackageTechStatus Status { get; set; } = status;
}

public class TrendsData
{
    public TrendSeries Battery { get; } = new("Battery");
    public TrendSeries Flow { get; } = new("Flow");
    public TrendSeries Paw { get; } = new("Paw");
    public TrendSeries Vol { get; } = new("Vol");
    public TrendSeries PIP { get; } = new("PIP");
    public TrendSeries PEEP { get; } = new("PEEP");
    public TrendSeries MVTotal { get; } = new("MVTotal");
    public TrendSeries MVMachine { get; } = new("MVMachine");
    public TrendSeries Vt { get; } = new("Vt");
    public TrendSeries RB1 { get; } = new("RB1");
    public TrendSeries RB2 { get; } = new("RB2");
    public TrendSeries Cst { get; } = new("Cst");
    public TrendSeries Rst { get; } = new("Rst");
    public TrendSeries Leak { get; } = new("Leak");

    public IEnumerable<TrendSeries> AllSeries => [Battery, Flow, Paw, Vol, PIP, PEEP, MVTotal, MVMachine, Vt, RB1, RB2, Cst, Rst, Leak];

    public TrendSeries? GetSeriesByName(string name)
    {
        return AllSeries.FirstOrDefault(s => s.DisplayName.Equals(name, StringComparison.CurrentCultureIgnoreCase));
    }

    public void ClearAll()
    {
        foreach (var series in AllSeries)
        {
            series.Clear();
        }
    }
}

public class TrendSeries(string name)
{
    public string DisplayName { get; } = name;
    public ObservableCollection<DataPoint> Points { get; } = [];
    
    public void Clear()
    {
        Points.Clear();
    }

    public void Add(double x, double y)
    {
        Points.Add(new DataPoint(x, y));
    }
    
    public double GetValueAtIndexSafe(int idx)
    {
        if (idx < 0 || idx >= Points.Count)
        {
            return double.NaN;
        }
        return Points[idx].Y;
    }
}