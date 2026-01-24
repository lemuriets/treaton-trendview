using System.Collections.ObjectModel;
using LogDecoder.CAN.General;
using LogDecoder.GUI.Models;
using LogDecoder.Helpers.TimeHelper;
using LogDecoder.Parser;

namespace LogDecoder.GUI.Services;

public class DataProvider(LogParser parser)
{
    public void GetDataForTimeSpan(TrendsData seriesPool, ObservableCollection<LogMessage> messages, DateTime start, int lengthSec, double startX)
    {
        var x = startX;
        var packages = parser.GetPackagesForTimeSpan(parser.IdsAll, start, lengthSec);
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